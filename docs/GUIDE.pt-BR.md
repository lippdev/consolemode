# Guia do Console Mode

Detalhes que não cabem no [README](../README.pt-BR.md). 🇺🇸 [Guide in English](GUIDE.md)

## Modos e restauração

| Modo | Ao sair |
|------|---------|
| **Steam Big Picture** | Restauração automática (app na bandeja) |
| **Playnite tela cheia** | Restauração automática (app na bandeja) |
| **Modo Xbox** | Manual — *Restaurar agora*, menu da bandeja ou reabrir a janela |

Também dá para restaurar a qualquer momento pela bandeja (*Restaurar setup* / *Mostrar janela*). Com cortinas pretas, **ESC** remove o overlay.

`consolemode://stop`, `ConsoleMode.exe --stop` e o atalho configuravel **Voltar ao PC** fecham o Playnite em tela cheia antes de restaurar. As acoes **Voltar ao PC** e **Sair do Console Mode** do menu da sessao, e **Restaurar setup** da bandeja, mantem o comportamento de restauracao; na previa do menu, **Voltar ao PC** apenas fecha a previa.

## API de controle local

Os links `consolemode://` não dão resposta. Ferramentas que precisam de uma — um agente de controle remoto rodando como serviço do Windows, um plugin de Stream Deck que mostra se o modo console está ligado — podem usar o named pipe `\\.\pipe\ConsoleMode.Control` com o app aberto: envie uma linha JSON e receba outra.

```
→ {"cmd":"status"}          // ou "start", "stop", "show"
← {"ok":true,"active":true,"restoring":false,"mode":"xboxMode","version":"1.5.0"}
```

`start`, `stop` e `show` fazem o mesmo que o link `consolemode://` correspondente e respondem quando o app terminou (`ok:false` com `error` se o modo console não entrou ou a restauração não terminou). `status` só consulta. Só o usuário logado e o LocalSystem conseguem conectar; nada fica exposto na rede.

## Controle da TV

Ajustes → **TV** pode ligar a TV e trocar para a entrada HDMI do PC quando o modo console começa e, se você quiser, colocá-la em espera depois que a mesa volta. Uma TV que não responde nunca trava o modo console: o app registra no log e espera a tela de jogo como sempre.

Com o padrão, **Não controlar**, nada é enviado e nada é acrescentado ao iniciar nem ao restaurar. Com um caminho escolhido, a etapa da TV roda primeiro quando o modo console começa e tem limite de **30 segundos** (o pior caso, quando a TV não responde e o Wake-on-LAN é tentado); a espera opcional ao restaurar tem limite de **15 segundos**. Uma TV que já está ligada costuma responder em um ou dois segundos.

A maioria das placas de vídeo de PC não envia HDMI-CEC, então o app fala com a TV pela rede:

### Google TV / Android TV

TVs TCL, Sony, Hisense, Philips e outras com Google TV ou Android TV, via ADB (o protocolo de depuração do Android). Nada para instalar no PC.

1. Na TV: **Configurações → Sistema → Sobre**, aperte **Build do Android TV OS** 7 vezes para liberar as Opções do desenvolvedor.
2. **Configurações → Sistema → Opções do desenvolvedor**: ative **Depuração USB** (em algumas TVs, **Depuração pela rede** / **ADB pela rede**).
3. No Console Mode, escolha *Google TV / Android TV*, informe o IP da TV (Configurações → Rede na TV; reserve esse IP no roteador) e a entrada HDMI do PC.
4. Aperte **Testar agora**. A TV pergunta "Permitir depuração deste computador?": marque **Sempre permitir** e aperte **Permitir**.

A chave ADB deste PC fica guardada criptografada para o seu usuário do Windows (DPAPI), então a TV só pergunta uma vez. Se a pasta de dados for copiada para outro usuário do Windows ou outro PC, a chave não pode ser lida lá: o app cria outra e a TV pergunta "Permitir depuração?" de novo.

Para acordar, o app usa a tecla de despertar do Android e depois a tecla **HDMI 1-4**. Se a sua TV ignorar essa tecla, preencha **Comando da entrada** com qualquer comando de shell do Android que abra a entrada do PC. Se a TV sai da rede em espera, informe o **endereço MAC** para o app mandar Wake-on-LAN antes (a opção "Ligar pela rede" / "Wake on Wi-Fi" da TV precisa estar ativa).

A "Depuração sem fio" com código de pareamento (Android 11+ em celulares) é outro protocolo, com TLS, e não é suportada: use a depuração USB / pela rede.

### LG webOS

TVs LG de 2014 em diante, pela rede local (a mesma API do app LG ThinQ). Nada para instalar no PC.

1. Na TV, ative **Ligar via Wi-Fi** (em modelos antigos: **LG Connect Apps** / **Mobile TV On**) para ela poder ser acordada da espera.
2. No Console Mode, escolha *LG webOS*, informe o IP da TV, o **endereço MAC** (Configurações → Rede → Wi-Fi/Cabeada → Avançado na TV) e a entrada HDMI do PC.
3. Aperte **Testar agora** e aceite o pedido na TV. A chave que a TV devolve fica guardada (criptografada para o seu usuário do Windows com DPAPI), então ela só pergunta uma vez; se a pasta de dados for copiada para outro usuário do Windows ou outro PC, a chave não pode ser lida lá e a TV pergunta de novo.

A TV liga por Wake-on-LAN e troca de entrada com `ssap://tv/switchInput` (`HDMI_1` a `HDMI_4`); a espera usa `ssap://system/turnOff`. O app tenta primeiro `wss://` com criptografia na porta 3001. Depois da primeira autorização, ele guarda o certificado da TV e recusa um certificado diferente antes de enviar a chave salva. Confira o endereço da TV antes da primeira autorização: o certificado autoassinado passa a ser confiável nesse primeiro pareamento. TVs antigas que só aceitam `ws://` sem criptografia na porta 3000 exigem ativar **Permitir conexão webOS antiga sem criptografia** em Ajustes. Nessa porta, a chave de pareamento passa em texto puro pela rede; use apenas em uma rede confiável. Se o certificado ou a TV mudar, confirme o endereço, use **Esquecer pareamento** e autorize novamente na TV. Chaves de versões anteriores do Console Mode não são reutilizadas automaticamente; autorize o pareamento novamente.

## Extras opcionais

### HDR

Ativa HDR no monitor de foco enquanto o modo console estiver ligado. Ao sair, o estado anterior é restaurado.

### VRR

O Console Mode pode alterar a opção de VRR do Windows. Para melhor resultado, ligue também VRR / G-SYNC / FreeSync no **painel do driver da GPU** (NVIDIA ou AMD).

### Limite de FPS (RTSS)

Limita a taxa de quadros global durante o modo console (útil em TV 60 Hz). Exige RTSS instalado e em execução. O limite anterior volta ao sair.

## Limitações

- Layouts multi-monitor variam; em alguns setups a restauração pode precisar de uma nova tentativa pela bandeja
- O Modo Xbox não detecta o fim do fullscreen — restaure manualmente
- O limite de FPS é global (limitação do RTSS), não por tela
- O build WinUI 3 precisa ser compilado no Windows (`net8.0-windows`)

## Solução de problemas

### O layout da área de trabalho não foi restaurado

Abra o menu da bandeja e escolha **Restaurar setup**. Se o layout ainda estiver errado, escolha **Restaurar setup** novamente depois que o Windows terminar de aplicar a alteração do monitor. Você também pode reabrir a janela pela bandeja e restaurar manualmente.

### O áudio permaneceu na saída anterior

Confira se a saída de destino está conectada e disponível no Windows antes de iniciar o modo console. Para saídas HDMI/TV, pode ser necessário reconectar o cabo e iniciar o modo novamente.

### O controle aparece mas não faz nada (DualSense / DualShock)

Abra **Ajustes → Testar controle**: ele mostra ao vivo o que o Windows entrega de cada controle. Se a leitura fica vazia enquanto você aperta botões, quase sempre é o Steam capturando o controle (Steam aberto com suporte a PlayStation no Steam Input vira teclado/mouse no desktop). Feche o Steam, ou desligue o suporte a PlayStation no Steam Input, e teste de novo. Se continuar vazio, use **Copiar diagnóstico** e cole no formulário de feedback.

### HDR ou VRR não mudaram

Confirme se o monitor de foco é compatível com o recurso e se o HDR está ativado no Windows. Para VRR, ative também G-SYNC ou FreeSync no painel de controle da GPU quando aplicável.

## Compilar no Windows

Precisa do [Visual Studio 2022](https://visualstudio.microsoft.com/) com a workload **Desenvolvimento de aplicativos da Windows**, ou do SDK do .NET 8 + Windows App SDK.

```powershell
# Baixa o rtss-cli e compila os dois pacotes
.\build\Publish-ConsoleMode.ps1 -Version 1.4.0
```

Saída: `dist\ConsoleMode-Portable-x64.exe` e `dist\ConsoleMode-Setup-x64.exe` (o instalador precisa do [Inno Setup 6](https://jrsoftware.org/isinfo.php): `winget install JRSoftware.InnoSetup`). Abra `ConsoleMode.sln` para depurar.

Para lançar uma versão, faça o merge do PR de release na `main` e rode Actions → **Release** → Run workflow na `main` com a versão (como `1.4.0`, ou `1.4.0-beta.2` para pré-release) e **publish** marcado: o workflow cria a tag e a release no GitHub, gera os dois arquivos e os publica, e o app oferece a atualização. Sem "publish" ele só gera e guarda os arquivos como artefato. Fazer push de uma tag `v*` continua funcionando e faz o mesmo.

A implementação antiga em PowerShell + WPF (1.2 e anteriores) fica na branch [`legacy`](https://github.com/lippdev/consolemode/tree/legacy) e não é usada pelo app WinUI.
