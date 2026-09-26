# Guia do Console Mode

Detalhes que não cabem no [README](../README.pt-BR.md). 🇺🇸 [Guide in English](GUIDE.md)

## Modos e restauração

| Modo | Ao sair |
|------|---------|
| **Steam Big Picture** | Restauração automática (app na bandeja) |
| **Playnite tela cheia** | Restauração automática (app na bandeja) |
| **Modo Xbox** | Manual — *Restaurar agora*, menu da bandeja ou reabrir a janela |

Também dá para restaurar a qualquer momento pela bandeja (*Restaurar setup* / *Mostrar janela*). Com cortinas pretas, **ESC** remove o overlay.

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

### Home Assistant

Se você já usa o [Home Assistant](https://www.home-assistant.io/), ele alcança quase qualquer TV (HDMI-CEC por um Raspberry Pi, LG, Samsung, Sony, Roku, Android TV…). O Console Mode só executa uma entidade lá quando o modo console começa, e outra ao restaurar.

1. No Home Assistant, crie um **script** que liga a TV e escolhe a entrada do PC (por exemplo `media_player.turn_on` seguido de `media_player.select_source`) e, se quiser, outro que a desliga.
2. Crie um **token de acesso de longa duração**: seu perfil → **Segurança** → *Tokens de acesso de longa duração*.
3. No Console Mode, escolha *Home Assistant* e preencha o endereço (ex.: `http://homeassistant.local:8123`), o token e a entidade a executar (ex.: `script.ligar_tv_pc`). Aperte **Testar agora**.

Scripts e cenas rodam com `turn_on`, automações com `trigger`, botões com `press`; as outras entidades (`media_player`, `switch`…) com `turn_on` e, ao restaurar, com `turn_off` quando não há entidade de restauração. O token fica guardado criptografado para o seu usuário do Windows (DPAPI).

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

Para lançar uma versão, faça push de uma tag como `v1.4.0` (ou `v1.4.0-beta.2` para pré-release): o workflow `Release` gera e publica os dois arquivos, e o app oferece a atualização.

A implementação antiga em PowerShell + WPF (1.2 e anteriores) fica na branch [`legacy`](https://github.com/lippdev/consolemode/tree/legacy) e não é usada pelo app WinUI.
