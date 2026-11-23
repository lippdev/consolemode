use windows::{
    Win32::{
        Devices::FunctionDiscovery::{PKEY_Device_DeviceDesc, PKEY_DeviceInterface_FriendlyName},
        Foundation::{PROPERTYKEY, RPC_E_CHANGED_MODE},
        Media::Audio::*,
        System::Com::{StructuredStorage::*, *},
        UI::Shell::PropertiesSystem::IPropertyStore,
    },
    core::{PWSTR, Result},
};

#[derive(Debug, Clone)]
pub struct AudioOutput {
    pub endpoint_id: String,
    pub friendly_name: String,
    /// The name-based ID used by 1.7 configs; endpoint_id remains unambiguous.
    pub friendly_id: String,
    pub is_default: bool,
    pub is_active: bool,
}
fn display_name(endpoint: &str, adapter: &str) -> String {
    if endpoint.trim().is_empty() {
        adapter.into()
    } else if !adapter.trim().is_empty() && adapter != endpoint {
        format!("{endpoint} ({adapter})")
    } else {
        endpoint.into()
    }
}
fn friendly_id(endpoint: &str, adapter: &str) -> String {
    format!(r"{adapter}\Device\{endpoint}\Render")
}
struct Apartment(bool);
impl Drop for Apartment {
    fn drop(&mut self) {
        if self.0 {
            unsafe {
                CoUninitialize();
            }
        }
    }
}
unsafe fn owned_string(value: PWSTR) -> String {
    unsafe {
        let s = value.to_string().unwrap_or_default();
        CoTaskMemFree(Some(value.0.cast()));
        s
    }
}
unsafe fn property(store: &IPropertyStore, key: &PROPERTYKEY) -> String {
    unsafe {
        let Ok(mut value) = store.GetValue(key) else {
            return String::new();
        };
        let text = PropVariantToStringAlloc(&value)
            .map(|p| owned_string(p))
            .unwrap_or_default();
        let _ = PropVariantClear(&mut value);
        text
    }
}
/// Enumerate all render endpoints. The default is the console role, as in 1.7.
pub fn list_outputs() -> Result<Vec<AudioOutput>> {
    unsafe {
        let init = CoInitializeEx(None, COINIT_MULTITHREADED);
        if init != RPC_E_CHANGED_MODE {
            init.ok()?;
        }
        let _apartment = Apartment(init.is_ok());
        let enumerator: IMMDeviceEnumerator =
            CoCreateInstance(&MMDeviceEnumerator, None, CLSCTX_ALL)?;
        let default = enumerator
            .GetDefaultAudioEndpoint(eRender, eConsole)
            .ok()
            .and_then(|d| d.GetId().ok())
            .map(|p| owned_string(p));
        let devices = enumerator.EnumAudioEndpoints(eRender, DEVICE_STATE(DEVICE_STATEMASK_ALL))?;
        let mut outputs = vec![];
        for i in 0..devices.GetCount()? {
            let device = devices.Item(i)?;
            let id = owned_string(device.GetId()?);
            let store = device.OpenPropertyStore(STGM_READ)?;
            let endpoint = property(&store, &PKEY_Device_DeviceDesc);
            let adapter = property(&store, &PKEY_DeviceInterface_FriendlyName);
            outputs.push(AudioOutput {
                is_default: default
                    .as_ref()
                    .is_some_and(|d| d.eq_ignore_ascii_case(&id)),
                is_active: device.GetState()? == DEVICE_STATE_ACTIVE,
                endpoint_id: id,
                friendly_name: display_name(&endpoint, &adapter),
                friendly_id: friendly_id(&endpoint, &adapter),
            });
        }
        outputs.sort_by(|a, b| {
            b.is_active
                .cmp(&a.is_active)
                .then_with(|| a.friendly_name.cmp(&b.friendly_name))
        });
        Ok(outputs)
    }
}
#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn legacy_audio_names() {
        assert_eq!(
            display_name("Speakers", "Realtek(R) Audio"),
            "Speakers (Realtek(R) Audio)"
        );
        assert_eq!(display_name("", "Adapter"), "Adapter");
        assert_eq!(display_name("Speakers", ""), "Speakers");
        assert_eq!(display_name("Same", "Same"), "Same");
        assert_eq!(
            friendly_id("Speakers", "Realtek(R) Audio"),
            r"Realtek(R) Audio\Device\Speakers\Render"
        );
    }
}
