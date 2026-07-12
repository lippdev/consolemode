#Requires -Version 5.1
# Console Mode - Interface gráfica (wizard) em WPF

Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$script:TrayIcon = $null
$script:LoadedMonitors = @()
$script:AppIcon = $null
$script:WizardStep = 0
$script:AllowFormClosePrompt = $false
$script:ConsoleUiLocked = $false
$script:MonitorLoopBusy = $false
$script:LastStatusMessage = ""
$script:ConsoleMonitorTimer = $null
$script:AudioOnConnectId = "__on_connect__"
$script:AudioOnConnectLabel = "Usar áudio ao conectar (TV/monitor)"
$script:MonitorRows = @()
$script:Ui = @{}

$script:Theme = @{
    Bg          = "#141417"
    Surface     = "#1D1D21"
    Card        = "#232329"
    Input       = "#2C2C33"
    InputHover  = "#37373F"
    Border      = "#3A3A42"
    Text        = "#EDEDF0"
    Muted       = "#9B9BA6"
    Accent      = "#3ECFA0"
    AccentDark  = "#173B31"
    AccentHover = "#58DCB0"
    Warning     = "#E0BA66"
    Success     = "#4EC9B0"
}

$script:FpsPresetValues = @(30, 48, 50, 59, 60, 72, 75, 90, 120, 144)
$script:FpsCustomComboValue = -1

# ---------------------------------------------------------------------------
# Helpers visuais
# ---------------------------------------------------------------------------

function New-UiBrush {
    param([Parameter(Mandatory)][string]$Hex)
    $brush = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.ColorConverter]::ConvertFromString($Hex))
    $brush.Freeze()
    return $brush
}

function Get-ConsoleAppIcon {
    if ($script:AppIcon) { return $script:AppIcon }

    try {
        $assembly = [Reflection.Assembly]::GetExecutingAssembly()
        $location = $assembly.Location
        if ($location -and (Test-Path -LiteralPath $location) -and $location -like '*.exe') {
            $script:AppIcon = [System.Drawing.Icon]::ExtractAssociatedIcon($location)
            if ($script:AppIcon) { return $script:AppIcon }
        }
    }
    catch { }

    try {
        $psExe = (Get-Process -Id $PID).Path
        if ($psExe) {
            $script:AppIcon = [System.Drawing.Icon]::ExtractAssociatedIcon($psExe)
        }
    }
    catch { }

    return $script:AppIcon
}

function Get-ConsoleAppImageSource {
    $icon = Get-ConsoleAppIcon
    if (-not $icon) { return $null }
    try {
        return [System.Windows.Interop.Imaging]::CreateBitmapSourceFromHIcon(
            $icon.Handle,
            [System.Windows.Int32Rect]::Empty,
            [System.Windows.Media.Imaging.BitmapSizeOptions]::FromEmptyOptions()
        )
    }
    catch { return $null }
}

function Move-WindowToPrimaryScreen {
    param($Window)

    # SystemParameters.WorkArea = área de trabalho do monitor primário em DIPs
    $wa = [System.Windows.SystemParameters]::WorkArea
    $Window.Left = $wa.Left + [Math]::Max(0, ($wa.Width - $Window.Width) / 2)
    $Window.Top = $wa.Top + [Math]::Max(0, ($wa.Height - $Window.Height) / 2)
}

function Show-FormOnPrimary {
    param($Form)

    $Form.ShowInTaskbar = $true
    $Form.WindowState = [System.Windows.WindowState]::Normal
    Move-WindowToPrimaryScreen -Window $Form
    $Form.Show()
    [void]$Form.Activate()
}

function Hide-ConsoleFormForActiveMode {
    param($Form)
    $Form.ShowInTaskbar = $false
    $Form.Hide()
}

function Show-StatusMessage {
    param(
        $Form,
        $StatusLabel,
        [string]$Message,
        [string]$Color = $null,
        [switch]$Force
    )

    if (-not $Force -and $script:LastStatusMessage -eq $Message) { return }
    $script:LastStatusMessage = $Message
    $StatusLabel.Text = $Message
    if ($Color) { $StatusLabel.Foreground = New-UiBrush $Color }
}

function Show-UiMessageBox {
    param(
        [string]$Message,
        [string]$Title = "Console Mode",
        [string]$Buttons = "OK",
        [string]$Icon = "None"
    )
    return [System.Windows.MessageBox]::Show(
        $Message, $Title,
        [System.Windows.MessageBoxButton]$Buttons,
        [System.Windows.MessageBoxImage]$Icon
    )
}

# ---------------------------------------------------------------------------
# Rótulos e conversões (sem UI)
# ---------------------------------------------------------------------------

function Get-MonitorFriendlyLabel {
    param($Monitor)

    $winLabel = if ($Monitor.WindowsDisplayNumber -gt 0) {
        "Monitor $($Monitor.WindowsDisplayNumber)"
    }
    else {
        ($Monitor.Name -replace '\\\\\.\\', '').Trim()
    }

    if ($Monitor.MonitorName) {
        return "$($Monitor.MonitorName) ($winLabel)"
    }
    return $winLabel
}

function Get-MonitorSecondaryLabel {
    param($Monitor)
    $parts = @()
    if ($Monitor.Resolution) { $parts += ($Monitor.Resolution -replace '\s+X\s+', ' x ') }
    if ($Monitor.Frequency) { $parts += "$($Monitor.Frequency) Hz" }
    if (-not $Monitor.IsActive) { $parts += "Desconectado" }
    return ($parts -join "  •  ")
}

function Get-HideStrategyLabel {
    param([string]$Strategy)

    switch ($Strategy) {
        "blackCurtain" { return "Cortinas pretas" }
        "turnOff" { return "Desligar fisicamente (DDC/CI)" }
        default { return "Desconectar monitores" }
    }
}

function Get-FullscreenModeLabel {
    param([string]$Mode)
    if ($Mode -eq "xboxMode") { return "Modo Xbox (Win+F11) — Alpha" }
    if ($Mode -eq "playnite") { return "Playnite (modo tela cheia)" }
    return "Steam Big Picture (recomendado)"
}

function Test-ConsoleWatchNeeded {
    param([string]$FullscreenMode)

    if ($FullscreenMode -eq "xboxMode") { return $false }
    return $true
}

# ---------------------------------------------------------------------------
# XAML
# ---------------------------------------------------------------------------

$script:MainXaml = @'
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Console Mode" Width="880" Height="820"
        WindowStartupLocation="Manual" ResizeMode="CanMinimize"
        Background="#141417" TextOptions.TextFormattingMode="Display"
        FontFamily="Segoe UI Variable Text, Segoe UI" FontSize="13.5">
  <Window.Resources>
    <SolidColorBrush x:Key="BgBrush" Color="#141417"/>
    <SolidColorBrush x:Key="SurfaceBrush" Color="#1D1D21"/>
    <SolidColorBrush x:Key="CardBrush" Color="#232329"/>
    <SolidColorBrush x:Key="InputBrush" Color="#2C2C33"/>
    <SolidColorBrush x:Key="InputHoverBrush" Color="#37373F"/>
    <SolidColorBrush x:Key="BorderBrush" Color="#3A3A42"/>
    <SolidColorBrush x:Key="TextBrush" Color="#EDEDF0"/>
    <SolidColorBrush x:Key="MutedBrush" Color="#9B9BA6"/>
    <SolidColorBrush x:Key="AccentBrush" Color="#3ECFA0"/>
    <SolidColorBrush x:Key="AccentDarkBrush" Color="#173B31"/>
    <SolidColorBrush x:Key="AccentHoverBrush" Color="#58DCB0"/>
    <SolidColorBrush x:Key="WarningBrush" Color="#E0BA66"/>
    <SolidColorBrush x:Key="DarkTextBrush" Color="#10241D"/>

    <Style x:Key="IconText" TargetType="TextBlock">
      <Setter Property="FontFamily" Value="Segoe Fluent Icons, Segoe MDL2 Assets"/>
      <Setter Property="VerticalAlignment" Value="Center"/>
      <Setter Property="Foreground" Value="{StaticResource MutedBrush}"/>
    </Style>

    <Style x:Key="Card" TargetType="Border">
      <Setter Property="Background" Value="{StaticResource SurfaceBrush}"/>
      <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}"/>
      <Setter Property="BorderThickness" Value="1"/>
      <Setter Property="CornerRadius" Value="12"/>
    </Style>

    <Style x:Key="Btn" TargetType="Button">
      <Setter Property="Foreground" Value="{StaticResource TextBrush}"/>
      <Setter Property="Background" Value="{StaticResource InputBrush}"/>
      <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}"/>
      <Setter Property="Cursor" Value="Hand"/>
      <Setter Property="Height" Value="40"/>
      <Setter Property="Padding" Value="16,0"/>
      <Setter Property="Template">
        <Setter.Value>
          <ControlTemplate TargetType="Button">
            <Border x:Name="bd" Background="{TemplateBinding Background}"
                    BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="1" CornerRadius="10">
              <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"
                                Margin="{TemplateBinding Padding}"/>
            </Border>
            <ControlTemplate.Triggers>
              <Trigger Property="IsMouseOver" Value="True">
                <Setter TargetName="bd" Property="Background" Value="{StaticResource InputHoverBrush}"/>
              </Trigger>
              <Trigger Property="IsEnabled" Value="False">
                <Setter Property="Opacity" Value="0.4"/>
              </Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>

    <Style x:Key="BtnPrimary" TargetType="Button" BasedOn="{StaticResource Btn}">
      <Setter Property="Foreground" Value="{StaticResource DarkTextBrush}"/>
      <Setter Property="FontWeight" Value="SemiBold"/>
      <Setter Property="Template">
        <Setter.Value>
          <ControlTemplate TargetType="Button">
            <Border x:Name="bd" Background="{StaticResource AccentBrush}" CornerRadius="10">
              <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"
                                Margin="{TemplateBinding Padding}"/>
            </Border>
            <ControlTemplate.Triggers>
              <Trigger Property="IsMouseOver" Value="True">
                <Setter TargetName="bd" Property="Background" Value="{StaticResource AccentHoverBrush}"/>
              </Trigger>
              <Trigger Property="IsEnabled" Value="False">
                <Setter Property="Opacity" Value="0.4"/>
              </Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>

    <Style x:Key="TabBtn" TargetType="Button">
      <Setter Property="Foreground" Value="{StaticResource MutedBrush}"/>
      <Setter Property="Background" Value="Transparent"/>
      <Setter Property="Cursor" Value="Hand"/>
      <Setter Property="Template">
        <Setter.Value>
          <ControlTemplate TargetType="Button">
            <Grid Background="Transparent">
              <Grid.RowDefinitions>
                <RowDefinition Height="*"/>
                <RowDefinition Height="3"/>
              </Grid.RowDefinitions>
              <ContentPresenter Grid.Row="0" HorizontalAlignment="Center" VerticalAlignment="Center" Margin="0,10"/>
              <Border Grid.Row="1" x:Name="underline" Background="{StaticResource AccentBrush}"
                      CornerRadius="2" Visibility="Hidden" Margin="18,0"/>
            </Grid>
            <ControlTemplate.Triggers>
              <Trigger Property="Tag" Value="active">
                <Setter TargetName="underline" Property="Visibility" Value="Visible"/>
                <Setter Property="Foreground" Value="{StaticResource AccentBrush}"/>
              </Trigger>
              <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Foreground" Value="{StaticResource TextBrush}"/>
              </Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>

    <Style TargetType="ComboBoxItem">
      <Setter Property="Foreground" Value="{StaticResource TextBrush}"/>
      <Setter Property="Padding" Value="10,7"/>
      <Setter Property="Template">
        <Setter.Value>
          <ControlTemplate TargetType="ComboBoxItem">
            <Border x:Name="bd" Background="Transparent" CornerRadius="6" Margin="4,1">
              <ContentPresenter Margin="{TemplateBinding Padding}"/>
            </Border>
            <ControlTemplate.Triggers>
              <Trigger Property="IsHighlighted" Value="True">
                <Setter TargetName="bd" Property="Background" Value="{StaticResource InputHoverBrush}"/>
              </Trigger>
              <Trigger Property="IsSelected" Value="True">
                <Setter TargetName="bd" Property="Background" Value="{StaticResource AccentDarkBrush}"/>
                <Setter Property="Foreground" Value="{StaticResource AccentBrush}"/>
              </Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>

    <Style TargetType="ComboBox">
      <Setter Property="Foreground" Value="{StaticResource TextBrush}"/>
      <Setter Property="Height" Value="38"/>
      <Setter Property="Template">
        <Setter.Value>
          <ControlTemplate TargetType="ComboBox">
            <Grid>
              <ToggleButton x:Name="toggle" Focusable="False" ClickMode="Press"
                  IsChecked="{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}">
                <ToggleButton.Template>
                  <ControlTemplate TargetType="ToggleButton">
                    <Border x:Name="bd" Background="{StaticResource InputBrush}"
                            BorderBrush="{StaticResource BorderBrush}" BorderThickness="1" CornerRadius="9">
                      <TextBlock Text="&#xE70D;" FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets"
                                 FontSize="11" HorizontalAlignment="Right" VerticalAlignment="Center"
                                 Margin="0,0,12,0" Foreground="{StaticResource MutedBrush}"/>
                    </Border>
                    <ControlTemplate.Triggers>
                      <Trigger Property="IsMouseOver" Value="True">
                        <Setter TargetName="bd" Property="Background" Value="{StaticResource InputHoverBrush}"/>
                      </Trigger>
                    </ControlTemplate.Triggers>
                  </ControlTemplate>
                </ToggleButton.Template>
              </ToggleButton>
              <ContentPresenter Margin="14,0,32,0" VerticalAlignment="Center" IsHitTestVisible="False"
                                Content="{TemplateBinding SelectionBoxItem}"
                                ContentTemplate="{TemplateBinding SelectionBoxItemTemplate}"
                                ContentTemplateSelector="{TemplateBinding ItemTemplateSelector}"/>
              <Popup IsOpen="{TemplateBinding IsDropDownOpen}" Placement="Bottom" AllowsTransparency="True"
                     StaysOpen="False" PopupAnimation="Fade">
                <Border Background="{StaticResource CardBrush}" BorderBrush="{StaticResource BorderBrush}"
                        BorderThickness="1" CornerRadius="9" MaxHeight="260" Margin="0,4,0,0"
                        MinWidth="{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}">
                  <ScrollViewer VerticalScrollBarVisibility="Auto">
                    <ItemsPresenter Margin="0,4"/>
                  </ScrollViewer>
                </Border>
              </Popup>
            </Grid>
            <ControlTemplate.Triggers>
              <Trigger Property="IsEnabled" Value="False">
                <Setter Property="Opacity" Value="0.4"/>
              </Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>

    <Style TargetType="CheckBox">
      <Setter Property="Foreground" Value="{StaticResource TextBrush}"/>
      <Setter Property="Cursor" Value="Hand"/>
      <Setter Property="VerticalContentAlignment" Value="Center"/>
      <Setter Property="Template">
        <Setter.Value>
          <ControlTemplate TargetType="CheckBox">
            <StackPanel Orientation="Horizontal" Background="Transparent">
              <Border x:Name="box" Width="21" Height="21" CornerRadius="6"
                      Background="{StaticResource InputBrush}"
                      BorderBrush="{StaticResource BorderBrush}" BorderThickness="1.5"
                      VerticalAlignment="Center">
                <TextBlock x:Name="check" Text="&#xE73E;" FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets"
                           FontSize="12" FontWeight="Bold" Foreground="{StaticResource DarkTextBrush}"
                           HorizontalAlignment="Center" VerticalAlignment="Center" Visibility="Hidden"/>
              </Border>
              <ContentPresenter Margin="9,0,0,0" VerticalAlignment="Center" RecognizesAccessKey="True"/>
            </StackPanel>
            <ControlTemplate.Triggers>
              <Trigger Property="IsChecked" Value="True">
                <Setter TargetName="box" Property="Background" Value="{StaticResource AccentBrush}"/>
                <Setter TargetName="box" Property="BorderBrush" Value="{StaticResource AccentBrush}"/>
                <Setter TargetName="check" Property="Visibility" Value="Visible"/>
              </Trigger>
              <Trigger Property="IsMouseOver" Value="True">
                <Setter TargetName="box" Property="BorderBrush" Value="{StaticResource AccentBrush}"/>
              </Trigger>
              <Trigger Property="IsEnabled" Value="False">
                <Setter Property="Opacity" Value="0.4"/>
              </Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>

    <Style TargetType="RadioButton">
      <Setter Property="Foreground" Value="{StaticResource TextBrush}"/>
      <Setter Property="Cursor" Value="Hand"/>
      <Setter Property="Template">
        <Setter.Value>
          <ControlTemplate TargetType="RadioButton">
            <StackPanel Orientation="Horizontal" Background="Transparent">
              <Border x:Name="outer" Width="21" Height="21" CornerRadius="11"
                      Background="{StaticResource InputBrush}"
                      BorderBrush="{StaticResource BorderBrush}" BorderThickness="1.5"
                      VerticalAlignment="Center">
                <Ellipse x:Name="dot" Width="9" Height="9" Fill="{StaticResource AccentBrush}"
                         HorizontalAlignment="Center" VerticalAlignment="Center" Visibility="Hidden"/>
              </Border>
              <ContentPresenter Margin="9,0,0,0" VerticalAlignment="Center" RecognizesAccessKey="True"/>
            </StackPanel>
            <ControlTemplate.Triggers>
              <Trigger Property="IsChecked" Value="True">
                <Setter TargetName="outer" Property="BorderBrush" Value="{StaticResource AccentBrush}"/>
                <Setter TargetName="dot" Property="Visibility" Value="Visible"/>
              </Trigger>
              <Trigger Property="IsMouseOver" Value="True">
                <Setter TargetName="outer" Property="BorderBrush" Value="{StaticResource AccentBrush}"/>
              </Trigger>
              <Trigger Property="IsEnabled" Value="False">
                <Setter Property="Opacity" Value="0.4"/>
              </Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>

    <Style TargetType="TextBox">
      <Setter Property="Foreground" Value="{StaticResource TextBrush}"/>
      <Setter Property="Background" Value="{StaticResource InputBrush}"/>
      <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}"/>
      <Setter Property="CaretBrush" Value="{StaticResource AccentBrush}"/>
      <Setter Property="Padding" Value="10,7"/>
      <Setter Property="Template">
        <Setter.Value>
          <ControlTemplate TargetType="TextBox">
            <Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}"
                    BorderThickness="1" CornerRadius="9">
              <ScrollViewer x:Name="PART_ContentHost" Margin="{TemplateBinding Padding}"/>
            </Border>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>

    <Style TargetType="ScrollBar">
      <Setter Property="Background" Value="Transparent"/>
      <Setter Property="Width" Value="8"/>
    </Style>
  </Window.Resources>

  <Grid Margin="26,18,26,18">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>

    <!-- Cabeçalho -->
    <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,4,0,10">
      <Border Width="46" Height="46" CornerRadius="12" Background="{StaticResource AccentDarkBrush}">
        <TextBlock Style="{StaticResource IconText}" Text="&#xE7F4;" FontSize="22"
                   Foreground="{StaticResource AccentBrush}" HorizontalAlignment="Center"/>
      </Border>
      <StackPanel Margin="14,0,0,0" VerticalAlignment="Center">
        <TextBlock Text="Console Mode" FontSize="24" FontWeight="Bold" Foreground="{StaticResource AccentBrush}"/>
        <TextBlock Text="Transforme seu PC em console de jogos em poucos passos."
                   Foreground="{StaticResource MutedBrush}" Margin="1,2,0,0"/>
      </StackPanel>
    </StackPanel>

    <!-- Tabs -->
    <Grid Grid.Row="1" Margin="0,0,0,14">
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="*"/>
      </Grid.ColumnDefinitions>
      <Button x:Name="Tab0" Grid.Column="0" Style="{StaticResource TabBtn}">
        <StackPanel Orientation="Horizontal">
          <TextBlock Style="{StaticResource IconText}" Foreground="{Binding Foreground, RelativeSource={RelativeSource AncestorType=Button}}" Text="&#xE7F4;" FontSize="15" Margin="0,0,8,0"/>
          <TextBlock Text="1. Monitores" FontWeight="SemiBold"/>
        </StackPanel>
      </Button>
      <Button x:Name="Tab1" Grid.Column="1" Style="{StaticResource TabBtn}">
        <StackPanel Orientation="Horizontal">
          <TextBlock Style="{StaticResource IconText}" Foreground="{Binding Foreground, RelativeSource={RelativeSource AncestorType=Button}}" Text="&#xE7FC;" FontSize="15" Margin="0,0,8,0"/>
          <TextBlock Text="2. Modo" FontWeight="SemiBold"/>
        </StackPanel>
      </Button>
      <Button x:Name="Tab2" Grid.Column="2" Style="{StaticResource TabBtn}">
        <StackPanel Orientation="Horizontal">
          <TextBlock Style="{StaticResource IconText}" Foreground="{Binding Foreground, RelativeSource={RelativeSource AncestorType=Button}}" Text="&#xE767;" FontSize="15" Margin="0,0,8,0"/>
          <TextBlock Text="3. Áudio" FontWeight="SemiBold"/>
        </StackPanel>
      </Button>
      <Button x:Name="Tab3" Grid.Column="3" Style="{StaticResource TabBtn}">
        <StackPanel Orientation="Horizontal">
          <TextBlock Style="{StaticResource IconText}" Foreground="{Binding Foreground, RelativeSource={RelativeSource AncestorType=Button}}" Text="&#xE768;" FontSize="15" Margin="0,0,8,0"/>
          <TextBlock Text="Iniciar" FontWeight="SemiBold"/>
        </StackPanel>
      </Button>
    </Grid>

    <!-- Conteúdo -->
    <Grid Grid.Row="2">

      <!-- Passo 1: Monitores -->
      <ScrollViewer x:Name="Step0" VerticalScrollBarVisibility="Auto">
        <StackPanel>
          <Border Style="{StaticResource Card}" Padding="16">
            <StackPanel>
              <Canvas x:Name="DiagramCanvas" Height="64" Margin="2,0,2,12"/>
              <Grid Margin="4,0,4,8">
                <Grid.ColumnDefinitions>
                  <ColumnDefinition Width="52"/>
                  <ColumnDefinition Width="80"/>
                  <ColumnDefinition Width="*"/>
                  <ColumnDefinition Width="240"/>
                </Grid.ColumnDefinitions>
                <TextBlock Grid.Column="0" Text="Foco" Foreground="{StaticResource MutedBrush}" FontWeight="SemiBold"/>
                <TextBlock Grid.Column="1" Text="Esconder" Foreground="{StaticResource MutedBrush}" FontWeight="SemiBold"/>
                <TextBlock Grid.Column="2" Text="Monitor" Foreground="{StaticResource MutedBrush}" FontWeight="SemiBold" Margin="8,0,0,0"/>
                <TextBlock Grid.Column="3" Text="Resolução / Hz" Foreground="{StaticResource MutedBrush}" FontWeight="SemiBold"/>
              </Grid>
              <StackPanel x:Name="MonitorList"/>
              <Grid Margin="0,12,0,0">
                <Grid.ColumnDefinitions>
                  <ColumnDefinition Width="Auto"/>
                  <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <Button x:Name="BtnHideOthers" Style="{StaticResource Btn}" Height="38">
                  <StackPanel Orientation="Horizontal">
                    <TextBlock Style="{StaticResource IconText}" Text="&#xED1A;" Margin="0,0,8,0"/>
                    <TextBlock Text="Esconder todos exceto o foco"/>
                  </StackPanel>
                </Button>
                <StackPanel Grid.Column="1" Orientation="Horizontal" Margin="14,0,0,0" VerticalAlignment="Center">
                  <TextBlock Style="{StaticResource IconText}" Text="&#xE946;" Margin="0,2,8,0" VerticalAlignment="Top"/>
                  <TextBlock Text="Desconectados: use modos do cache ou estimados — aplicados ao conectar. Serão reativados ao iniciar."
                             Foreground="{StaticResource MutedBrush}" TextWrapping="Wrap" FontSize="12" MaxWidth="420"/>
                </StackPanel>
              </Grid>
            </StackPanel>
          </Border>

          <Border Style="{StaticResource Card}" Padding="16" Margin="0,12,0,0">
            <StackPanel>
              <StackPanel Orientation="Horizontal" Margin="0,0,0,10">
                <TextBlock Style="{StaticResource IconText}" Text="&#xEC4A;" Foreground="{StaticResource AccentBrush}" Margin="0,0,8,0"/>
                <TextBlock Text="Limite de FPS (anti-tearing)" Foreground="{StaticResource AccentBrush}" FontWeight="SemiBold"/>
              </StackPanel>
              <StackPanel Orientation="Horizontal">
                <ComboBox x:Name="FpsCombo" Width="260" DisplayMemberPath="Text"/>
                <TextBox x:Name="FpsCustomBox" Width="90" Margin="10,0,0,0" Visibility="Collapsed"
                         VerticalContentAlignment="Center" Text="60"/>
              </StackPanel>
              <TextBlock x:Name="FpsStatusText" Foreground="{StaticResource MutedBrush}" FontSize="12"
                         TextWrapping="Wrap" Margin="2,10,0,0"/>
            </StackPanel>
          </Border>
        </StackPanel>
      </ScrollViewer>

      <!-- Passo 2: Modo -->
      <ScrollViewer x:Name="Step1" VerticalScrollBarVisibility="Auto" Visibility="Collapsed">
        <StackPanel>
          <Border Style="{StaticResource Card}" Padding="16">
            <StackPanel>
              <TextBlock Text="Como esconder os outros monitores?" Foreground="{StaticResource AccentBrush}"
                         FontWeight="SemiBold" Margin="0,0,0,10"/>
              <ComboBox x:Name="HideStrategyCombo" DisplayMemberPath="Text"/>
              <TextBlock Text="Desconectar: desativa no Windows (rápido). Cortinas: overlay preto. DDC/CI: apaga o painel via hardware."
                         Foreground="{StaticResource MutedBrush}" FontSize="12" TextWrapping="Wrap" Margin="2,10,0,0"/>
            </StackPanel>
          </Border>

          <Border Style="{StaticResource Card}" Padding="16" Margin="0,12,0,0">
            <StackPanel>
              <TextBlock Text="Modo de tela cheia" Foreground="{StaticResource AccentBrush}"
                         FontWeight="SemiBold" Margin="0,0,0,12"/>
              <RadioButton x:Name="ModeBigPicture" GroupName="fsmode">
                <TextBlock Text="Steam Big Picture (recomendado)" FontWeight="SemiBold"/>
              </RadioButton>
              <TextBlock Text="Abre a Steam em modo Big Picture no monitor de foco. Restauração automática ao sair."
                         Foreground="{StaticResource MutedBrush}" FontSize="12" Margin="30,4,0,14"/>
              <RadioButton x:Name="ModePlaynite" GroupName="fsmode">
                <TextBlock Text="Playnite (modo tela cheia)" FontWeight="SemiBold"/>
              </RadioButton>
              <TextBlock x:Name="PlayniteDesc" Foreground="{StaticResource MutedBrush}" FontSize="12" Margin="30,4,0,14"/>
              <RadioButton x:Name="ModeXbox" GroupName="fsmode">
                <TextBlock Text="Modo Xbox (Win+F11) — Alpha" FontWeight="SemiBold"/>
              </RadioButton>
              <TextBlock Text="Experimental: envia Win+F11. O app permanece aberto; restaure manualmente com Restaurar agora."
                         Foreground="{StaticResource WarningBrush}" FontSize="12" Margin="30,4,0,2"/>
            </StackPanel>
          </Border>

          <Border Style="{StaticResource Card}" Padding="16" Margin="0,12,0,0">
            <StackPanel>
              <TextBlock Text="Recursos de vídeo" Foreground="{StaticResource AccentBrush}"
                         FontWeight="SemiBold" Margin="0,0,0,12"/>
              <Grid>
                <Grid.ColumnDefinitions>
                  <ColumnDefinition Width="*"/>
                  <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <CheckBox x:Name="ChkHdr" Grid.Column="0" Content="Ativar HDR no monitor de foco"/>
                <CheckBox x:Name="ChkVrr" Grid.Column="1" Content="Ativar VRR (taxa de atualização variável)"/>
              </Grid>
              <StackPanel Orientation="Horizontal" Margin="0,12,0,0">
                <TextBlock Style="{StaticResource IconText}" Text="&#xE7BA;" Foreground="{StaticResource WarningBrush}"
                           FontSize="12" Margin="0,2,8,0" VerticalAlignment="Top"/>
                <TextBlock Text="VRR: esta opção aplica apenas o ajuste do Windows — recomendamos ligar o VRR pelo painel do driver da GPU (NVIDIA/AMD)."
                           Foreground="{StaticResource WarningBrush}" FontSize="12" TextWrapping="Wrap" MaxWidth="620"/>
              </StackPanel>
            </StackPanel>
          </Border>
        </StackPanel>
      </ScrollViewer>

      <!-- Passo 3: Áudio -->
      <StackPanel x:Name="Step2" Visibility="Collapsed">
        <Border Style="{StaticResource Card}" Padding="16">
          <StackPanel>
            <TextBlock Text="Saída de áudio" Foreground="{StaticResource AccentBrush}"
                       FontWeight="SemiBold" Margin="0,0,0,10"/>
            <ComboBox x:Name="AudioCombo" DisplayMemberPath="Text"/>
            <TextBlock x:Name="AudioHintText" Foreground="{StaticResource MutedBrush}" FontSize="12"
                       TextWrapping="Wrap" Margin="2,10,0,0"
                       Text="A saída escolhida é aplicada ao iniciar e restaurada ao sair. 'Usar áudio ao conectar' troca automaticamente quando a TV conectar."/>
          </StackPanel>
        </Border>
      </StackPanel>

      <!-- Passo 4: Revisão / Iniciar -->
      <StackPanel x:Name="Step3" Visibility="Collapsed">
        <Border Style="{StaticResource Card}" Padding="20">
          <StackPanel>
            <StackPanel Orientation="Horizontal" Margin="0,0,0,14">
              <TextBlock Style="{StaticResource IconText}" Text="&#xE930;" Foreground="{StaticResource AccentBrush}"
                         FontSize="18" Margin="0,0,10,0"/>
              <TextBlock Text="Revisão" FontSize="17" FontWeight="Bold" Foreground="{StaticResource TextBrush}"/>
            </StackPanel>
            <TextBlock x:Name="ReviewText" Foreground="{StaticResource TextBrush}" FontSize="14"
                       LineHeight="26" TextWrapping="Wrap"/>
          </StackPanel>
        </Border>
      </StackPanel>

      <!-- Modo ativo -->
      <StackPanel x:Name="PanelActive" Visibility="Collapsed">
        <Border Style="{StaticResource Card}" Padding="24">
          <StackPanel>
            <StackPanel Orientation="Horizontal" Margin="0,0,0,12">
              <TextBlock Style="{StaticResource IconText}" Text="&#xE768;" Foreground="{StaticResource AccentBrush}"
                         FontSize="20" Margin="0,0,10,0"/>
              <TextBlock Text="Modo console ativo" FontSize="19" FontWeight="Bold" Foreground="{StaticResource TextBrush}"/>
            </StackPanel>
            <TextBlock x:Name="ActiveDescText" Foreground="{StaticResource MutedBrush}" FontSize="13.5"
                       TextWrapping="Wrap" LineHeight="22"/>
          </StackPanel>
        </Border>
      </StackPanel>
    </Grid>

    <!-- Status -->
    <StackPanel Grid.Row="3" Orientation="Horizontal" Margin="4,14,0,12">
      <TextBlock x:Name="StatusIcon" Style="{StaticResource IconText}" Text="&#xE73E;" FontSize="14"
                 Foreground="{StaticResource AccentBrush}" Margin="0,0,8,0"/>
      <TextBlock x:Name="StatusText" Text="Pronto." Foreground="{StaticResource MutedBrush}"/>
    </StackPanel>

    <!-- Botões -->
    <Grid Grid.Row="4">
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
      </Grid.ColumnDefinitions>
      <Button x:Name="BtnBack" Grid.Column="0" Style="{StaticResource Btn}" Width="120" AutomationProperties.Name="Voltar">
        <StackPanel Orientation="Horizontal">
          <TextBlock Style="{StaticResource IconText}" Text="&#xE76B;" FontSize="12" Margin="0,0,8,0"/>
          <TextBlock Text="Voltar"/>
        </StackPanel>
      </Button>
      <Button x:Name="BtnSave" Grid.Column="1" Style="{StaticResource BtnPrimary}" Width="130" Margin="12,0,0,0">
        <StackPanel Orientation="Horizontal">
          <TextBlock Style="{StaticResource IconText}" Text="&#xE74E;" FontSize="13" Margin="0,0,8,0"
                     Foreground="{StaticResource DarkTextBrush}"/>
          <TextBlock Text="Salvar"/>
        </StackPanel>
      </Button>
      <Button x:Name="BtnRefresh" Grid.Column="2" Style="{StaticResource Btn}" Width="140" Margin="12,0,0,0">
        <StackPanel Orientation="Horizontal">
          <TextBlock Style="{StaticResource IconText}" Text="&#xE72C;" FontSize="13" Margin="0,0,8,0"/>
          <TextBlock Text="Atualizar"/>
        </StackPanel>
      </Button>
      <Button x:Name="BtnRestore" Grid.Column="3" Style="{StaticResource Btn}" Width="170" Margin="12,0,0,0" IsEnabled="False">
        <StackPanel Orientation="Horizontal">
          <TextBlock Style="{StaticResource IconText}" Text="&#xE7A7;" FontSize="13" Margin="0,0,8,0"/>
          <TextBlock Text="Restaurar agora"/>
        </StackPanel>
      </Button>
      <Button x:Name="BtnNext" Grid.Column="5" Style="{StaticResource BtnPrimary}" Width="150" AutomationProperties.Name="Proximo">
        <StackPanel Orientation="Horizontal">
          <TextBlock Text="Próximo"/>
          <TextBlock Style="{StaticResource IconText}" Text="&#xE76C;" FontSize="12" Margin="8,0,0,0"
                     Foreground="{StaticResource DarkTextBrush}"/>
        </StackPanel>
      </Button>
      <Button x:Name="BtnStart" Grid.Column="5" Style="{StaticResource BtnPrimary}" Width="230" Visibility="Collapsed" AutomationProperties.Name="IniciarModoConsole">
        <StackPanel Orientation="Horizontal">
          <TextBlock Style="{StaticResource IconText}" Text="&#xE768;" FontSize="13" Margin="0,0,8,0"
                     Foreground="{StaticResource DarkTextBrush}"/>
          <TextBlock Text="Iniciar modo console" FontWeight="SemiBold"/>
        </StackPanel>
      </Button>
    </Grid>
  </Grid>
</Window>
'@

# ---------------------------------------------------------------------------
# Construção das linhas de monitores
# ---------------------------------------------------------------------------

function New-MonitorRow {
    param(
        $Monitor,
        [string]$SavedFocus,
        [string[]]$SavedHide,
        $SavedMode
    )

    $card = New-Object System.Windows.Controls.Border
    $card.Background = New-UiBrush $script:Theme.Card
    $card.CornerRadius = New-Object System.Windows.CornerRadius 10
    $card.Padding = New-Object System.Windows.Thickness 12, 10, 12, 10
    $card.Margin = New-Object System.Windows.Thickness 0, 0, 0, 8

    $grid = New-Object System.Windows.Controls.Grid
    foreach ($w in @(52, 80, 0, 240)) {
        $col = New-Object System.Windows.Controls.ColumnDefinition
        if ($w -gt 0) { $col.Width = New-Object System.Windows.GridLength $w }
        [void]$grid.ColumnDefinitions.Add($col)
    }

    $radio = New-Object System.Windows.Controls.RadioButton
    $radio.GroupName = "focusMonitor"
    $radio.Tag = $Monitor.Name
    $radio.VerticalAlignment = "Center"
    $radio.IsChecked = ($Monitor.Name -eq $SavedFocus)
    [System.Windows.Controls.Grid]::SetColumn($radio, 0)
    [void]$grid.Children.Add($radio)

    $check = New-Object System.Windows.Controls.CheckBox
    $check.Tag = $Monitor.Name
    $check.VerticalAlignment = "Center"
    $check.IsChecked = ($SavedHide -contains $Monitor.Name)
    $check.IsEnabled = [bool]$Monitor.IsActive
    [System.Windows.Controls.Grid]::SetColumn($check, 1)
    [void]$grid.Children.Add($check)

    $namePanel = New-Object System.Windows.Controls.StackPanel
    $namePanel.Orientation = "Horizontal"
    $namePanel.Margin = New-Object System.Windows.Thickness 8, 0, 8, 0

    $icon = New-Object System.Windows.Controls.TextBlock
    $icon.FontFamily = New-Object System.Windows.Media.FontFamily "Segoe Fluent Icons, Segoe MDL2 Assets"
    $icon.FontSize = 20
    $icon.VerticalAlignment = "Center"
    $icon.Margin = New-Object System.Windows.Thickness 0, 0, 12, 0
    if ($Monitor.IsActive) {
        $icon.Text = [char]0xE7F4
        $icon.Foreground = New-UiBrush $script:Theme.Text
    }
    else {
        $icon.Text = [char]0xEA14
        $icon.Foreground = New-UiBrush $script:Theme.Muted
    }
    [void]$namePanel.Children.Add($icon)

    $textPanel = New-Object System.Windows.Controls.StackPanel
    $textPanel.VerticalAlignment = "Center"

    $titleRow = New-Object System.Windows.Controls.StackPanel
    $titleRow.Orientation = "Horizontal"

    $nameText = New-Object System.Windows.Controls.TextBlock
    $nameText.Text = Get-MonitorFriendlyLabel -Monitor $Monitor
    $nameText.FontWeight = "SemiBold"
    $nameText.Foreground = New-UiBrush ($(if ($Monitor.IsActive) { $script:Theme.Text } else { $script:Theme.Muted }))
    [void]$titleRow.Children.Add($nameText)

    if ($Monitor.IsPrimary -and $Monitor.IsActive) {
        $badge = New-Object System.Windows.Controls.Border
        $badge.Background = New-UiBrush $script:Theme.AccentDark
        $badge.CornerRadius = New-Object System.Windows.CornerRadius 6
        $badge.Padding = New-Object System.Windows.Thickness 8, 2, 8, 2
        $badge.Margin = New-Object System.Windows.Thickness 10, 0, 0, 0
        $badge.VerticalAlignment = "Center"
        $badgeText = New-Object System.Windows.Controls.TextBlock
        $badgeText.Text = "PRIMÁRIO"
        $badgeText.FontSize = 10.5
        $badgeText.FontWeight = "Bold"
        $badgeText.Foreground = New-UiBrush $script:Theme.Accent
        $badge.Child = $badgeText
        [void]$titleRow.Children.Add($badge)
    }
    [void]$textPanel.Children.Add($titleRow)

    $subText = New-Object System.Windows.Controls.TextBlock
    $subText.Text = Get-MonitorSecondaryLabel -Monitor $Monitor
    $subText.FontSize = 12
    $subText.Foreground = New-UiBrush $script:Theme.Muted
    $subText.Margin = New-Object System.Windows.Thickness 0, 2, 0, 0
    [void]$textPanel.Children.Add($subText)

    [void]$namePanel.Children.Add($textPanel)
    [System.Windows.Controls.Grid]::SetColumn($namePanel, 2)
    [void]$grid.Children.Add($namePanel)

    $combo = New-Object System.Windows.Controls.ComboBox
    $combo.Tag = $Monitor.Name
    $combo.DisplayMemberPath = "Text"
    $combo.VerticalAlignment = "Center"
    Initialize-MonitorModeCombo -Combo $combo -Monitor $Monitor -SavedMode $SavedMode
    [System.Windows.Controls.Grid]::SetColumn($combo, 3)
    [void]$grid.Children.Add($combo)

    $card.Child = $grid

    return @{
        Name  = [string]$Monitor.Name
        Card  = $card
        Radio = $radio
        Check = $check
        Combo = $combo
    }
}

function Build-MonitorPanel {
    param(
        [array]$Monitors,
        [string]$SavedFocus,
        [string[]]$SavedHide,
        [hashtable]$SavedMonitorModes = @{}
    )

    $list = $script:Ui.MonitorList
    $list.Children.Clear()
    $script:MonitorRows = @()

    if ($Monitors.Count -eq 0) {
        $lbl = New-Object System.Windows.Controls.TextBlock
        $lbl.Text = "Nenhum monitor detectado."
        $lbl.Foreground = New-UiBrush $script:Theme.Muted
        [void]$list.Children.Add($lbl)
        return
    }

    foreach ($monitor in $Monitors) {
        $savedMode = if ($SavedMonitorModes -and $SavedMonitorModes.ContainsKey($monitor.Name)) {
            $SavedMonitorModes[$monitor.Name]
        } else { $null }

        $row = New-MonitorRow -Monitor $monitor -SavedFocus $SavedFocus -SavedHide $SavedHide -SavedMode $savedMode
        $script:MonitorRows += $row
        [void]$list.Children.Add($row.Card)

        $row.Radio.Add_Checked({
            $sel = Get-GuiSelections
            if ($sel.FocusMonitor) {
                Build-MonitorLayoutDiagram -Monitors $script:LoadedMonitors -FocusName $sel.FocusMonitor
            }
        })
    }
}

function Initialize-MonitorModeCombo {
    param(
        $Combo,
        $Monitor,
        $SavedMode = $null
    )

    $Combo.Items.Clear()
    [void]$Combo.Items.Add([PSCustomObject]@{
        Text       = "(Manter atual)"
        UseCurrent = $true
        Width      = 0
        Height     = 0
        Frequency  = 0
    })

    $Combo.IsEnabled = $true
    $modes = @(Get-MonitorDisplayModes -MonitorName $Monitor.Name -Monitor $Monitor)
    $matched = $false

    if (-not $Monitor.IsActive -and $modes.Count -eq 0) {
        $Combo.SelectedIndex = 0
        return
    }

    foreach ($mode in $modes) {
        [void]$Combo.Items.Add([PSCustomObject]@{
            Text       = [string]$mode.Text
            UseCurrent = $false
            Width      = [int]$mode.Width
            Height     = [int]$mode.Height
            Frequency  = [int]$mode.Frequency
        })

        if ($SavedMode -and -not $matched) {
            if ([int]$mode.Width -eq [int]$SavedMode.Width -and
                [int]$mode.Height -eq [int]$SavedMode.Height -and
                [int]$mode.Frequency -eq [int]$SavedMode.Frequency) {
                $matched = $true
            }
        }
    }

    if ($SavedMode -and [int]$SavedMode.Width -gt 0 -and [int]$SavedMode.Height -gt 0 -and -not $matched) {
        $label = Get-MonitorModeLabel -Mode $SavedMode
        [void]$Combo.Items.Add([PSCustomObject]@{
            Text       = "$label (salvo)"
            UseCurrent = $false
            Width      = [int]$SavedMode.Width
            Height     = [int]$SavedMode.Height
            Frequency  = [int]$SavedMode.Frequency
        })
        $Combo.SelectedIndex = $Combo.Items.Count - 1
        return
    }

    if ($SavedMode -and [int]$SavedMode.Width -gt 0 -and [int]$SavedMode.Height -gt 0) {
        for ($i = 0; $i -lt $Combo.Items.Count; $i++) {
            $item = $Combo.Items[$i]
            if ($item.UseCurrent) { continue }
            if ([int]$item.Width -eq [int]$SavedMode.Width -and
                [int]$item.Height -eq [int]$SavedMode.Height -and
                [int]$item.Frequency -eq [int]$SavedMode.Frequency) {
                $Combo.SelectedIndex = $i
                return
            }
        }
    }

    $Combo.SelectedIndex = 0
}

function Get-MonitorModeFromCombo {
    param($Combo)

    $item = $Combo.SelectedItem
    if (-not $item -or $item.UseCurrent) { return $null }

    return @{
        Width     = [int]$item.Width
        Height    = [int]$item.Height
        Frequency = [int]$item.Frequency
    }
}

function Get-MonitorModesFromPanel {
    $modes = @{}
    foreach ($row in $script:MonitorRows) {
        $mode = Get-MonitorModeFromCombo -Combo $row.Combo
        if ($mode) {
            $modes[[string]$row.Name] = $mode
        }
    }
    return $modes
}

function Get-GuiSelections {
    $focusMonitor = ""
    $hideMonitors = [System.Collections.ArrayList]@()

    foreach ($row in $script:MonitorRows) {
        if ($row.Radio.IsChecked) { $focusMonitor = [string]$row.Name }
        if ($row.Check.IsChecked) { [void]$hideMonitors.Add([string]$row.Name) }
    }

    return @{
        FocusMonitor = $focusMonitor
        HideMonitors = @($hideMonitors)
        MonitorModes = Get-MonitorModesFromPanel
    }
}

# ---------------------------------------------------------------------------
# Diagrama de layout
# ---------------------------------------------------------------------------

function Build-MonitorLayoutDiagram {
    param(
        [array]$Monitors,
        [string]$FocusName
    )

    $canvas = $script:Ui.DiagramCanvas
    $canvas.Children.Clear()

    if ($Monitors.Count -eq 0) { return }

    $active = @($Monitors | Where-Object { $_.IsActive -and $_.Width -gt 0 -and $_.Height -gt 0 })
    if ($active.Count -eq 0) { $active = @($Monitors) }

    $minX = ($active | ForEach-Object {
        if ($_.LeftTop -match '(-?\d+)\s*,\s*(-?\d+)') { [int]$Matches[1] } else { 0 }
    } | Measure-Object -Minimum).Minimum
    $minY = ($active | ForEach-Object {
        if ($_.LeftTop -match '(-?\d+)\s*,\s*(-?\d+)') { [int]$Matches[2] } else { 0 }
    } | Measure-Object -Minimum).Minimum

    $maxX = 0
    $maxY = 0
    foreach ($m in $active) {
        $x = 0; $y = 0
        if ($m.LeftTop -match '(-?\d+)\s*,\s*(-?\d+)') {
            $x = [int]$Matches[1]
            $y = [int]$Matches[2]
        }
        $w = if ($m.Width) { [int]$m.Width } else { 1920 }
        $h = if ($m.Height) { [int]$m.Height } else { 1080 }
        $maxX = [Math]::Max($maxX, ($x - $minX) + $w)
        $maxY = [Math]::Max($maxY, ($y - $minY) + $h)
    }
    if ($maxX -le 0) { $maxX = 1920 }
    if ($maxY -le 0) { $maxY = 1080 }

    $availW = [Math]::Max(120.0, $canvas.ActualWidth)
    if ($availW -le 120) { $availW = 640.0 }
    $availH = [Math]::Max(40.0, $canvas.Height)
    $scale = [Math]::Min($availW / $maxX, $availH / $maxY)

    foreach ($m in $active) {
        $x = 0; $y = 0
        if ($m.LeftTop -match '(-?\d+)\s*,\s*(-?\d+)') {
            $x = [int]$Matches[1] - $minX
            $y = [int]$Matches[2] - $minY
        }
        $w = if ($m.Width) { [int]$m.Width } else { 1920 }
        $h = if ($m.Height) { [int]$m.Height } else { 1080 }

        $isFocus = ($m.Name -eq $FocusName)

        $box = New-Object System.Windows.Controls.Border
        $box.Width = [Math]::Max(28, $w * $scale - 4)
        $box.Height = [Math]::Max(20, $h * $scale - 4)
        $box.CornerRadius = New-Object System.Windows.CornerRadius 6
        $box.BorderThickness = New-Object System.Windows.Thickness 1.5

        if ($isFocus) {
            $box.Background = New-UiBrush $script:Theme.AccentDark
            $box.BorderBrush = New-UiBrush $script:Theme.Accent
            $fg = New-UiBrush $script:Theme.Accent
        }
        elseif (-not $m.IsActive) {
            $box.Background = New-UiBrush $script:Theme.Card
            $box.BorderBrush = New-UiBrush $script:Theme.Border
            $fg = New-UiBrush $script:Theme.Muted
        }
        else {
            $box.Background = New-UiBrush $script:Theme.Input
            $box.BorderBrush = New-UiBrush $script:Theme.Border
            $fg = New-UiBrush $script:Theme.Text
        }

        $lbl = New-Object System.Windows.Controls.TextBlock
        $lbl.Text = if ($m.WindowsDisplayNumber -gt 0) {
            "$($m.WindowsDisplayNumber)"
        }
        else {
            ($m.Name -replace '\\\\\.\\DISPLAY', '').Trim()
        }
        $lbl.FontWeight = "Bold"
        $lbl.Foreground = $fg
        $lbl.HorizontalAlignment = "Center"
        $lbl.VerticalAlignment = "Center"
        $box.Child = $lbl

        [System.Windows.Controls.Canvas]::SetLeft($box, 2 + $x * $scale)
        [System.Windows.Controls.Canvas]::SetTop($box, 2 + $y * $scale)
        [void]$canvas.Children.Add($box)
    }
}

# ---------------------------------------------------------------------------
# FPS / Áudio / Estratégia
# ---------------------------------------------------------------------------

function Initialize-FpsLimitCombo {
    param($Combo)

    $Combo.Items.Clear()
    [void]$Combo.Items.Add([PSCustomObject]@{ Text = "(Não limitar)"; Value = 0 })
    foreach ($fps in $script:FpsPresetValues) {
        [void]$Combo.Items.Add([PSCustomObject]@{ Text = "$fps FPS"; Value = $fps })
    }
    [void]$Combo.Items.Add([PSCustomObject]@{ Text = "Personalizado"; Value = $script:FpsCustomComboValue })
}

function Select-FpsLimitInCombo {
    param(
        $Combo,
        $CustomBox,
        [int]$SavedLimit
    )

    if ($SavedLimit -le 0) {
        $Combo.SelectedIndex = 0
        $CustomBox.Visibility = "Collapsed"
        return
    }

    for ($i = 0; $i -lt $Combo.Items.Count; $i++) {
        if ([int]$Combo.Items[$i].Value -eq $SavedLimit) {
            $Combo.SelectedIndex = $i
            $CustomBox.Visibility = "Collapsed"
            return
        }
    }

    $CustomBox.Text = [string][Math]::Max(1, [Math]::Min(360, $SavedLimit))
    for ($i = 0; $i -lt $Combo.Items.Count; $i++) {
        if ([int]$Combo.Items[$i].Value -eq $script:FpsCustomComboValue) {
            $Combo.SelectedIndex = $i
            break
        }
    }
    $CustomBox.Visibility = "Visible"
}

function Get-FpsLimitFromControls {
    param($Combo, $CustomBox)

    $item = $Combo.SelectedItem
    if (-not $item) { return 0 }
    $value = [int]$item.Value
    if ($value -eq $script:FpsCustomComboValue) {
        $custom = 0
        if ([int]::TryParse([string]$CustomBox.Text, [ref]$custom)) {
            return [Math]::Max(0, [Math]::Min(360, $custom))
        }
        return 0
    }
    return $value
}

function Update-FpsLimitStatusLabel {
    param($StatusLabel)

    if (Test-ConsoleRtssReady) {
        $StatusLabel.Text = "Requer RivaTuner em execução. Limite global durante o modo console (restaurado ao sair)."
        $StatusLabel.Foreground = New-UiBrush $script:Theme.Muted
    }
    elseif (Test-RtssInstalled) {
        $StatusLabel.Text = "RTSS instalado, mas rtss-cli ausente. Execute build\Get-RtssCli.ps1."
        $StatusLabel.Foreground = New-UiBrush $script:Theme.Warning
    }
    else {
        $StatusLabel.Text = "Instale RivaTuner Statistics Server (MSI Afterburner) para usar limite de FPS."
        $StatusLabel.Foreground = New-UiBrush $script:Theme.Warning
    }
}

function Initialize-HideStrategyCombo {
    param($Combo, [string]$SavedStrategy)

    $Combo.Items.Clear()
    [void]$Combo.Items.Add([PSCustomObject]@{ Text = "Desconectar monitores (recomendado)"; Value = "disconnect" })
    [void]$Combo.Items.Add([PSCustomObject]@{ Text = "Cortinas pretas (overlay)"; Value = "blackCurtain" })
    [void]$Combo.Items.Add([PSCustomObject]@{ Text = "Desligar fisicamente (DDC/CI)"; Value = "turnOff" })

    $Combo.SelectedIndex = switch ($SavedStrategy) {
        "blackCurtain" { 1 }
        "turnOff" { 2 }
        default { 0 }
    }
}

function Get-HideStrategyFromCombo {
    param($Combo)

    $item = $Combo.SelectedItem
    if ($item) { return [string]$item.Value }
    return "disconnect"
}

function Populate-AudioCombo {
    param(
        $Combo,
        [string]$SavedAudioId,
        [string]$SavedAudioName,
        [bool]$SavedAudioAutoSwitch
    )

    $Combo.Items.Clear()
    [void]$Combo.Items.Add([PSCustomObject]@{ Text = "(Não mudar)"; Id = "" })
    [void]$Combo.Items.Add([PSCustomObject]@{ Text = $script:AudioOnConnectLabel; Id = $script:AudioOnConnectId })

    if (-not (Test-SoundVolumeViewAvailable)) {
        $Combo.SelectedIndex = 0
        return
    }

    $devices = @(Get-ConsoleAudioDevices | Where-Object { $_.IsActive })
    foreach ($device in $devices) {
        [void]$Combo.Items.Add([PSCustomObject]@{ Text = $device.Name; Id = $device.FriendlyId })
    }

    $selectedIndex = 0
    $targetId = if ($SavedAudioAutoSwitch) { $script:AudioOnConnectId } else { $SavedAudioId }
    for ($i = 0; $i -lt $Combo.Items.Count; $i++) {
        if ($Combo.Items[$i].Id -eq $targetId) {
            $selectedIndex = $i
            break
        }
    }
    $Combo.SelectedIndex = $selectedIndex
}

# ---------------------------------------------------------------------------
# Seleções do wizard / salvar / revisão
# ---------------------------------------------------------------------------

function Get-WizardSelections {
    $ui = $script:Ui
    $selection = Get-GuiSelections
    $hideStrategy = Get-HideStrategyFromCombo -Combo $ui.HideStrategyCombo
    $fullscreenMode = if ($ui.ModeBigPicture.IsChecked) { "bigPicture" }
        elseif ($ui.ModePlaynite.IsChecked) { "playnite" }
        else { "xboxMode" }

    $audioItem = $ui.AudioCombo.SelectedItem
    $audioId = if ($audioItem) { [string]$audioItem.Id } else { "" }
    $audioName = if ($audioItem) { [string]$audioItem.Text } else { "" }
    $audioAutoSwitch = ($audioId -eq $script:AudioOnConnectId -or $audioId -eq "__auto__")
    if ($audioAutoSwitch) {
        $audioId = ""
        $audioName = $script:AudioOnConnectLabel
    }

    $hideMonitors = @($selection.HideMonitors | Where-Object { $_ -ne $selection.FocusMonitor })
    if ($hideMonitors.Count -eq 0) {
        $hideMonitors = @($script:LoadedMonitors | Where-Object {
            $_.Name -ne $selection.FocusMonitor -and $_.IsActive
        } | ForEach-Object { $_.Name })
    }

    $focusInfo = $script:LoadedMonitors | Where-Object { $_.Name -eq $selection.FocusMonitor } | Select-Object -First 1
    $focusLabel = if ($focusInfo) { Get-MonitorFriendlyLabel -Monitor $focusInfo } else { $selection.FocusMonitor }

    $hideLabels = @()
    foreach ($name in $hideMonitors) {
        $m = $script:LoadedMonitors | Where-Object { $_.Name -eq $name } | Select-Object -First 1
        $hideLabels += if ($m) { Get-MonitorFriendlyLabel -Monitor $m } else { $name }
    }

    $audioHint = ""
    if ($focusInfo -and $focusInfo.MonitorName) {
        $audioHint = $focusInfo.MonitorName
    }

    $fpsLimit = Get-FpsLimitFromControls -Combo $ui.FpsCombo -CustomBox $ui.FpsCustomBox

    return @{
        FocusMonitor   = $selection.FocusMonitor
        FocusLabel     = $focusLabel
        HideMonitors   = $hideMonitors
        HideLabels     = $hideLabels
        HideStrategy   = $hideStrategy
        FullscreenMode = $fullscreenMode
        AudioDeviceId  = $audioId
        AudioDeviceName = $audioName
        AudioAutoSwitch = $audioAutoSwitch
        AudioDeviceHint = $audioHint
        FocusMonitorInfo = $focusInfo
        FpsLimit       = $fpsLimit
        MonitorModes   = $selection.MonitorModes
        HdrEnable      = [bool]$ui.ChkHdr.IsChecked
        VrrEnable      = [bool]$ui.ChkVrr.IsChecked
    }
}

function Save-FromWizard {
    $data = Get-WizardSelections

    if ([string]::IsNullOrWhiteSpace($data.FocusMonitor)) {
        [void](Show-UiMessageBox -Message "Selecione o monitor de foco (TV/console)." -Icon "Warning")
        return $false
    }

    Set-ConsoleConfig `
        -FocusMonitor $data.FocusMonitor `
        -HideMonitors $data.HideMonitors `
        -HideStrategy $data.HideStrategy `
        -FullscreenMode $data.FullscreenMode `
        -AudioDeviceId $data.AudioDeviceId `
        -AudioDeviceName $data.AudioDeviceName `
        -AudioAutoSwitch $data.AudioAutoSwitch `
        -FpsLimit $data.FpsLimit `
        -MonitorModes $data.MonitorModes `
        -HdrEnable $data.HdrEnable `
        -VrrEnable $data.VrrEnable

    Show-StatusMessage -Form $script:Ui.Window -StatusLabel $script:Ui.StatusText `
        -Message "Configuração salva." -Color $script:Theme.Success
    return $true
}

function Update-ReviewPanel {
    $data = Get-WizardSelections
    $hideText = if ($data.HideLabels.Count -gt 0) { $data.HideLabels -join ", " } else { "(nenhum)" }
    $audioText = if ($data.AudioAutoSwitch) {
        $script:AudioOnConnectLabel
    }
    elseif ($data.AudioDeviceName) {
        $data.AudioDeviceName
    }
    else {
        "(não mudar)"
    }

    $fpsText = Get-FpsLimitLabel -FpsLimit $data.FpsLimit

    $focusModeText = "(manter atual)"
    if ($data.MonitorModes -and $data.MonitorModes.ContainsKey($data.FocusMonitor)) {
        $focusModeText = Get-MonitorModeLabel -Mode $data.MonitorModes[$data.FocusMonitor]
    }

    $script:Ui.ReviewText.Text = @(
        "Monitor de foco:  $($data.FocusLabel)"
        "Modo do foco:  $focusModeText"
        "Esconder:  $hideText"
        "Estratégia:  $(Get-HideStrategyLabel -Strategy $data.HideStrategy)"
        "Modo:  $(Get-FullscreenModeLabel -Mode $data.FullscreenMode)"
        "Áudio:  $audioText"
        "Limite de FPS:  $fpsText"
        "HDR:  $(if ($data.HdrEnable) { 'ativar no foco' } else { 'não mudar' })     VRR:  $(if ($data.VrrEnable) { 'ativar' } else { 'não mudar' })"
    ) -join [Environment]::NewLine
}

# ---------------------------------------------------------------------------
# Navegação / estados
# ---------------------------------------------------------------------------

function Show-WizardStep {
    param([int]$Step)

    $ui = $script:Ui
    $script:WizardStep = $Step

    $panels = @($ui.Step0, $ui.Step1, $ui.Step2, $ui.Step3)
    for ($i = 0; $i -lt $panels.Count; $i++) {
        $panels[$i].Visibility = if ($i -eq $Step) { "Visible" } else { "Collapsed" }
    }

    $tabs = @($ui.Tab0, $ui.Tab1, $ui.Tab2, $ui.Tab3)
    for ($i = 0; $i -lt $tabs.Count; $i++) {
        $tabs[$i].Tag = if ($i -eq $Step) { "active" } else { $null }
    }

    if ($Step -eq 3) { Update-ReviewPanel }

    $ui.BtnBack.IsEnabled = ($Step -gt 0)
    $ui.BtnNext.Visibility = if ($Step -lt 3) { "Visible" } else { "Collapsed" }
    $ui.BtnStart.Visibility = if ($Step -eq 3) { "Visible" } else { "Collapsed" }
}

function Set-WizardEnabled {
    param([bool]$Enabled)

    $ui = $script:Ui
    if ($Enabled) {
        Show-WizardStep -Step $script:WizardStep
    }
    else {
        foreach ($p in @($ui.Step0, $ui.Step1, $ui.Step2, $ui.Step3)) {
            $p.Visibility = "Collapsed"
        }
    }
    foreach ($btn in @($ui.BtnBack, $ui.BtnNext, $ui.BtnStart, $ui.BtnSave, $ui.BtnRefresh)) {
        $btn.IsEnabled = $Enabled
    }
    if ($Enabled) { $ui.BtnBack.IsEnabled = ($script:WizardStep -gt 0) }
    $ui.PanelActive.Visibility = if ($Enabled) { "Collapsed" } else { "Visible" }
    $ui.BtnRestore.IsEnabled = -not $Enabled
}

function Show-ConsoleActiveView {
    param([string]$FullscreenMode = "bigPicture")

    Set-WizardEnabled -Enabled $false
    if ($FullscreenMode -eq "xboxMode") {
        Show-FormOnPrimary -Form $script:Ui.Window
        return
    }
    Hide-ConsoleFormForActiveMode -Form $script:Ui.Window
}

function Set-ActiveConsoleMessaging {
    param([string]$FullscreenMode)

    $ui = $script:Ui
    if ($FullscreenMode -eq "xboxMode") {
        $ui.ActiveDescText.Text = @(
            "Modo Xbox (Alpha): sem restauração automática."
            "O app permanece aberto — use Restaurar agora quando terminar."
        ) -join [Environment]::NewLine
        Show-StatusMessage -Form $ui.Window -StatusLabel $ui.StatusText `
            -Message "Modo Xbox (Alpha) ativo. Restaure manualmente ao sair." `
            -Color $script:Theme.Warning -Force
    }
    else {
        $appName = if ($FullscreenMode -eq "playnite") { "Playnite" } else { "Big Picture" }
        $ui.ActiveDescText.Text = "O app fica oculto na bandeja. Ao sair do $appName, monitores, áudio e FPS são restaurados automaticamente."
        Show-StatusMessage -Form $ui.Window -StatusLabel $ui.StatusText `
            -Message "Modo console ativo. Ao sair do $appName, tudo será restaurado." `
            -Color $script:Theme.Accent -Force
    }
}

# ---------------------------------------------------------------------------
# Bandeja e loop de acompanhamento
# ---------------------------------------------------------------------------

function Initialize-TrayIcon {
    param(
        $Form,
        [scriptblock]$OnRestore
    )

    if ($script:TrayIcon) { return }

    $script:TrayIcon = New-Object System.Windows.Forms.NotifyIcon
    $script:TrayIcon.Icon = Get-ConsoleAppIcon
    $script:TrayIcon.Text = "Console Mode"
    $script:TrayIcon.Visible = $true

    $menu = New-Object System.Windows.Forms.ContextMenuStrip
    $restoreItem = New-Object System.Windows.Forms.ToolStripMenuItem
    $restoreItem.Text = "Restaurar setup"
    $restoreItem.Add_Click($OnRestore)
    [void]$menu.Items.Add($restoreItem)

    $showItem = New-Object System.Windows.Forms.ToolStripMenuItem
    $showItem.Text = "Mostrar janela"
    $showItem.Add_Click({ Show-FormOnPrimary -Form $script:Ui.Window })
    [void]$menu.Items.Add($showItem)

    $exitItem = New-Object System.Windows.Forms.ToolStripMenuItem
    $exitItem.Text = "Sair"
    $exitItem.Add_Click({
        if ($Script:ConsoleState.IsActive) { Stop-ConsoleMode }
        $script:ConsoleUiLocked = $false
        $script:TrayIcon.Visible = $false
        $script:TrayIcon.Dispose()
        $script:AllowFormClosePrompt = $true
        $script:Ui.Window.Close()
    })
    [void]$menu.Items.Add($exitItem)

    $script:TrayIcon.ContextMenuStrip = $menu
    $script:TrayIcon.Add_DoubleClick({ Show-FormOnPrimary -Form $script:Ui.Window })
}

function Stop-MonitorWorker {
    if ($script:ConsoleMonitorTimer) {
        $script:ConsoleMonitorTimer.Stop()
        $script:ConsoleMonitorTimer = $null
    }
}

function Invoke-ConsoleMonitorExitUi {
    Stop-MonitorWorker

    # A janela precisa voltar SEMPRE, mesmo que a restauração falhe no meio
    $restoreError = $null
    try {
        Stop-ConsoleMode
    }
    catch {
        $restoreError = $_.Exception.Message
        Write-ConsoleLog "ExitUi: erro na restauração: $restoreError"
    }
    finally {
        $script:ConsoleUiLocked = $false
        try {
            Set-WizardEnabled -Enabled $true
            Show-FormOnPrimary -Form $script:Ui.Window
        }
        catch {
            Write-ConsoleLog "ExitUi: erro ao reexibir janela: $($_.Exception.Message)"
        }
    }

    if ($restoreError) {
        Show-StatusMessage -Form $script:Ui.Window -StatusLabel $script:Ui.StatusText `
            -Message "Erro ao restaurar: $restoreError — use Restaurar agora ou build\Restore-SetupNow.ps1." `
            -Color $script:Theme.Warning -Force
        $script:Ui.BtnRestore.IsEnabled = $true
    }
    else {
        Show-StatusMessage -Form $script:Ui.Window -StatusLabel $script:Ui.StatusText `
            -Message "Modo console encerrado. Setup restaurado." `
            -Color $script:Theme.Success -Force
    }
}

function Start-MonitorTimer {
    Stop-MonitorWorker

    $script:ConsoleMonitorTimer = New-Object System.Windows.Threading.DispatcherTimer
    $script:ConsoleMonitorTimer.Interval = [TimeSpan]::FromMilliseconds(1500)
    $script:ConsoleMonitorTimer.Add_Tick({
        if ($script:MonitorLoopBusy) { return }
        if (-not $Script:ConsoleState.IsActive) { return }

        $script:MonitorLoopBusy = $true
        try {
            $result = Update-ConsoleMonitorLoop
            if ($result -eq "exit") {
                $script:ConsoleMonitorTimer.Stop()
                Invoke-ConsoleMonitorExitUi
                return
            }

            if ($result -eq "running" -and $Script:ConsoleState.LastAudioSwitchName) {
                $audioName = $Script:ConsoleState.LastAudioSwitchName
                $Script:ConsoleState.LastAudioSwitchName = $null
                Show-StatusMessage -Form $script:Ui.Window -StatusLabel $script:Ui.StatusText `
                    -Message "Áudio alterado para: $audioName." -Color $script:Theme.Accent -Force
            }

            $script:ConsoleMonitorTimer.Interval = [TimeSpan]::FromMilliseconds([Math]::Max(1000, (Get-ConsoleMonitorPollDelayMs)))
        }
        catch {
            # Nunca deixar o app "sumir": registrar, parar o loop e devolver a UI
            Write-ConsoleLog "Loop: erro não tratado: $($_.Exception.Message)"
            try {
                if ($script:ConsoleMonitorTimer) { $script:ConsoleMonitorTimer.Stop() }
                $script:ConsoleUiLocked = $false
                Set-WizardEnabled -Enabled $true
                Show-FormOnPrimary -Form $script:Ui.Window
                $script:Ui.BtnRestore.IsEnabled = $true
                Show-StatusMessage -Form $script:Ui.Window -StatusLabel $script:Ui.StatusText `
                    -Message "Erro no acompanhamento: $($_.Exception.Message) — use Restaurar agora." `
                    -Color $script:Theme.Warning -Force
            }
            catch { }
        }
        finally {
            $script:MonitorLoopBusy = $false
        }
    })
    $script:ConsoleMonitorTimer.Start()
}

# ---------------------------------------------------------------------------
# Janela principal
# ---------------------------------------------------------------------------

function Show-ConsoleModeGui {
    if (-not (Test-MultiMonitorToolAvailable)) {
        [void](Show-UiMessageBox -Message "MultiMonitorTool.exe não encontrado.`n`nEm desenvolvimento: coloque na pasta do projeto.`nNo executável: será extraído em ConsoleMode_Data\tools na primeira execução." `
            -Title "Console Mode - Erro" -Icon "Error")
        return
    }

    $config = Get-ConsoleConfig
    $monitors = Get-ConsoleMonitors
    $script:LoadedMonitors = $monitors

    $reader = New-Object System.Xml.XmlNodeReader ([xml]$script:MainXaml)
    $window = [System.Windows.Markup.XamlReader]::Load($reader)

    $ui = @{ Window = $window }
    foreach ($name in @(
        'Tab0','Tab1','Tab2','Tab3',
        'Step0','Step1','Step2','Step3','PanelActive',
        'DiagramCanvas','MonitorList','BtnHideOthers',
        'FpsCombo','FpsCustomBox','FpsStatusText',
        'HideStrategyCombo','ModeBigPicture','ModePlaynite','ModeXbox','PlayniteDesc',
        'ChkHdr','ChkVrr','AudioCombo','AudioHintText',
        'ReviewText','ActiveDescText','StatusIcon','StatusText',
        'BtnBack','BtnSave','BtnRefresh','BtnRestore','BtnNext','BtnStart'
    )) {
        $ui[$name] = $window.FindName($name)
    }
    $script:Ui = $ui

    $iconSource = Get-ConsoleAppImageSource
    if ($iconSource) { $window.Icon = $iconSource }

    Move-WindowToPrimaryScreen -Window $window

    # --- Passo 1: monitores + FPS ---
    Build-MonitorPanel -Monitors $monitors `
        -SavedFocus $config.focusMonitor -SavedHide $config.hideMonitors `
        -SavedMonitorModes $config.monitorModes
    $initialFocus = if ($config.focusMonitor) { $config.focusMonitor } else { ($monitors | Select-Object -First 1).Name }

    $window.Add_ContentRendered({
        $sel = Get-GuiSelections
        $focus = if ($sel.FocusMonitor) { $sel.FocusMonitor } else { $script:LoadedMonitors | Select-Object -First 1 -ExpandProperty Name }
        Build-MonitorLayoutDiagram -Monitors $script:LoadedMonitors -FocusName $focus
    })

    Initialize-FpsLimitCombo -Combo $ui.FpsCombo
    Select-FpsLimitInCombo -Combo $ui.FpsCombo -CustomBox $ui.FpsCustomBox -SavedLimit ([int]$config.fpsLimit)
    Update-FpsLimitStatusLabel -StatusLabel $ui.FpsStatusText

    $ui.FpsCombo.Add_SelectionChanged({
        $item = $script:Ui.FpsCombo.SelectedItem
        $isCustom = ($item -and [int]$item.Value -eq $script:FpsCustomComboValue)
        $script:Ui.FpsCustomBox.Visibility = if ($isCustom) { "Visible" } else { "Collapsed" }
    })

    $ui.BtnHideOthers.Add_Click({
        $sel = Get-GuiSelections
        foreach ($row in $script:MonitorRows) {
            $row.Check.IsChecked = ($row.Name -ne $sel.FocusMonitor) -and $row.Check.IsEnabled
        }
        Build-MonitorLayoutDiagram -Monitors $script:LoadedMonitors -FocusName $sel.FocusMonitor
    })

    # --- Passo 2: estratégia + modo + extras ---
    Initialize-HideStrategyCombo -Combo $ui.HideStrategyCombo -SavedStrategy $config.hideStrategy

    $playniteAvailable = Test-PlayniteAvailable
    $ui.ModePlaynite.IsEnabled = $playniteAvailable
    $ui.PlayniteDesc.Text = if ($playniteAvailable) {
        "Abre o Playnite em tela cheia no monitor de foco. Restauração automática ao sair."
    } else {
        "Playnite não encontrado. Instale-o (playnite.link) para habilitar esta opção."
    }
    if (-not $playniteAvailable) {
        $ui.PlayniteDesc.Foreground = New-UiBrush $script:Theme.Warning
    }

    $ui.ModeXbox.IsChecked = ($config.fullscreenMode -eq "xboxMode")
    $ui.ModePlaynite.IsChecked = ($config.fullscreenMode -eq "playnite" -and $playniteAvailable)
    $ui.ModeBigPicture.IsChecked = -not ($ui.ModeXbox.IsChecked -or $ui.ModePlaynite.IsChecked)

    $ui.ChkHdr.IsChecked = ($config.hdrEnable -eq $true)
    $ui.ChkVrr.IsChecked = ($config.vrrEnable -eq $true)

    # --- Passo 3: áudio ---
    Populate-AudioCombo -Combo $ui.AudioCombo `
        -SavedAudioId $config.audioDeviceId `
        -SavedAudioName $config.audioDeviceName `
        -SavedAudioAutoSwitch $config.audioAutoSwitch

    # --- Navegação ---
    $ui.BtnBack.Add_Click({
        if ($script:WizardStep -gt 0) { Show-WizardStep -Step ($script:WizardStep - 1) }
    })

    $ui.BtnNext.Add_Click({
        if ($script:WizardStep -eq 0) {
            $sel = Get-GuiSelections
            if ([string]::IsNullOrWhiteSpace($sel.FocusMonitor)) {
                [void](Show-UiMessageBox -Message "Selecione o monitor de foco." -Icon "Warning")
                return
            }
        }
        if ($script:WizardStep -lt 3) { Show-WizardStep -Step ($script:WizardStep + 1) }
    })

    $tabAction = {
        param($sender, $e)
        if ($script:ConsoleUiLocked) { return }
        $target = [int]($sender.Name -replace 'Tab', '')
        if ($target -gt 0) {
            $sel = Get-GuiSelections
            if ([string]::IsNullOrWhiteSpace($sel.FocusMonitor)) {
                [void](Show-UiMessageBox -Message "Selecione o monitor de foco." -Icon "Warning")
                return
            }
        }
        Show-WizardStep -Step $target
    }
    foreach ($tab in @($ui.Tab0, $ui.Tab1, $ui.Tab2, $ui.Tab3)) {
        $tab.Add_Click($tabAction)
    }

    $ui.BtnSave.Add_Click({ Save-FromWizard | Out-Null })

    $ui.BtnRefresh.Add_Click({
        Clear-ConsoleDeviceCache
        $refreshed = Get-ConsoleMonitors -ForceRefresh
        $script:LoadedMonitors = $refreshed
        $cfg = Get-ConsoleConfig
        $currentModes = Get-MonitorModesFromPanel
        $savedModes = if ($currentModes.Count -gt 0) { $currentModes } else { $cfg.monitorModes }
        Build-MonitorPanel -Monitors $refreshed `
            -SavedFocus $cfg.focusMonitor -SavedHide $cfg.hideMonitors `
            -SavedMonitorModes $savedModes
        Select-FpsLimitInCombo -Combo $script:Ui.FpsCombo -CustomBox $script:Ui.FpsCustomBox -SavedLimit ([int]$cfg.fpsLimit)
        Populate-AudioCombo -Combo $script:Ui.AudioCombo `
            -SavedAudioId $cfg.audioDeviceId `
            -SavedAudioName $cfg.audioDeviceName `
            -SavedAudioAutoSwitch $cfg.audioAutoSwitch
        $sel = Get-GuiSelections
        $focus = if ($sel.FocusMonitor) { $sel.FocusMonitor } else { ($refreshed | Select-Object -First 1).Name }
        Build-MonitorLayoutDiagram -Monitors $refreshed -FocusName $focus
        Update-FpsLimitStatusLabel -StatusLabel $script:Ui.FpsStatusText
        Show-StatusMessage -Form $script:Ui.Window -StatusLabel $script:Ui.StatusText `
            -Message "Listas atualizadas." -Color $script:Theme.Success
    })

    $restoreAction = {
        Stop-MonitorWorker
        Request-ConsoleModeExit
        Stop-ConsoleMode
        $script:ConsoleUiLocked = $false
        Set-WizardEnabled -Enabled $true
        Show-FormOnPrimary -Form $script:Ui.Window
        Show-StatusMessage -Form $script:Ui.Window -StatusLabel $script:Ui.StatusText `
            -Message "Setup restaurado." -Color $script:Theme.Success -Force
    }

    $startConsoleAction = {
        if (-not (Save-FromWizard)) { return }

        $data = Get-WizardSelections
        try {
            $script:ConsoleUiLocked = $true
            Initialize-TrayIcon -Form $script:Ui.Window -OnRestore $restoreAction

            Start-ConsoleMode `
                -FocusMonitor $data.FocusMonitor `
                -HideMonitors $data.HideMonitors `
                -HideStrategy $data.HideStrategy `
                -FullscreenMode $data.FullscreenMode `
                -AudioDeviceId $data.AudioDeviceId `
                -AudioAutoSwitch:$data.AudioAutoSwitch `
                -AudioDeviceHint $data.AudioDeviceHint `
                -FocusMonitorInfo $data.FocusMonitorInfo `
                -FpsLimit $data.FpsLimit `
                -MonitorModes $data.MonitorModes `
                -HdrEnable $data.HdrEnable `
                -VrrEnable $data.VrrEnable

            if ($data.FpsLimit -gt 0 -and -not $Script:ConsoleState.RtssLimitApplied) {
                [void](Show-UiMessageBox -Message "O limite de FPS não foi aplicado (RTSS ausente ou indisponível).`nO modo console continuará normalmente." -Icon "Warning")
            }

            Show-ConsoleActiveView -FullscreenMode $data.FullscreenMode
            Set-ActiveConsoleMessaging -FullscreenMode $data.FullscreenMode
            if (Test-ConsoleWatchNeeded -FullscreenMode $data.FullscreenMode) {
                Start-MonitorTimer
            }
            $script:Ui.BtnRestore.IsEnabled = $true
        }
        catch {
            [void](Show-UiMessageBox -Message "Erro ao iniciar modo console:`n$_" -Icon "Error")
            Stop-ConsoleMode
            $script:ConsoleUiLocked = $false
            Set-WizardEnabled -Enabled $true
            Show-FormOnPrimary -Form $script:Ui.Window
        }
    }

    $ui.BtnStart.Add_Click($startConsoleAction)
    $ui.BtnRestore.Add_Click($restoreAction)
    Initialize-TrayIcon -Form $window -OnRestore $restoreAction

    # --- Fechamento ---
    $window.Add_Closing({
        param($sender, $e)

        if (($Script:ConsoleState.IsActive -or $script:ConsoleUiLocked) -and -not $script:AllowFormClosePrompt) {
            $e.Cancel = $true

            if ($sender.IsVisible) {
                if ($Script:ConsoleState.FullscreenMode -eq "xboxMode") {
                    $answer = Show-UiMessageBox -Message "O modo Xbox está ativo. Restaurar o setup e sair?" `
                        -Buttons "YesNoCancel" -Icon "Question"
                    if ($answer -ne [System.Windows.MessageBoxResult]::Yes) {
                        return
                    }
                }
                else {
                    $answer = Show-UiMessageBox -Message "O modo console está ativo. Restaurar o setup e sair?`n`nNão = manter oculto na bandeja." `
                        -Buttons "YesNoCancel" -Icon "Question"
                    if ($answer -eq [System.Windows.MessageBoxResult]::Cancel) {
                        return
                    }
                    if ($answer -eq [System.Windows.MessageBoxResult]::No) {
                        Hide-ConsoleFormForActiveMode -Form $sender
                        return
                    }
                }
                Stop-MonitorWorker
                Stop-ConsoleMode
                $script:ConsoleUiLocked = $false
                $script:AllowFormClosePrompt = $true
                $e.Cancel = $false
                return
            }
            return
        }

        Stop-MonitorWorker
        if ($Script:ConsoleState.IsActive -or $Script:ConsoleState.RtssLimitApplied) {
            Stop-ConsoleMode
        }
        if ($script:TrayIcon) {
            $script:TrayIcon.Visible = $false
            $script:TrayIcon.Dispose()
        }
    })

    Show-WizardStep -Step 0

    $app = New-Object System.Windows.Application
    $app.ShutdownMode = [System.Windows.ShutdownMode]::OnMainWindowClose
    [void]$app.Run($window)
}
