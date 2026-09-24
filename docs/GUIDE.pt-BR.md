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
- Monitores e áudio dependem das ferramentas [NirSoft](https://www.nirsoft.net/) incluídas no pacote
- O limite de FPS é global (limitação do RTSS), não por tela
- O build WinUI 3 precisa ser compilado no Windows (`net8.0-windows`)

## Solução de problemas

**O controle aparece mas não faz nada (DualSense / DualShock):** abra **Ajustes → Testar controle**; ele mostra ao vivo o que o Windows entrega de cada controle. Se a leitura fica vazia enquanto você aperta botões, quase sempre é o Steam capturando o controle (Steam aberto com suporte a PlayStation no Steam Input vira teclado/mouse no desktop). Feche o Steam, ou desligue o suporte a PlayStation no Steam Input, e teste de novo. Se continuar vazio, use **Copiar diagnóstico** e cole no formulário de feedback.

## Compilar no Windows

Precisa do [Visual Studio 2022](https://visualstudio.microsoft.com/) com a workload **Desenvolvimento de aplicativos da Windows**, ou do SDK do .NET 8 + Windows App SDK.

```powershell
.\build\Publish-ConsoleMode.ps1 -Version 1.4.0
```

Saída: `dist\ConsoleMode-Portable-x64.exe` e `dist\ConsoleMode-Setup-x64.exe` (o instalador precisa do [Inno Setup 6](https://jrsoftware.org/isinfo.php): `winget install JRSoftware.InnoSetup`). Abra `ConsoleMode.sln` para depurar.

Para lançar uma versão, faça push de uma tag como `v1.4.0` (ou `v1.4.0-beta.2` para pré-release): o workflow `Release` gera e publica os dois arquivos, e o app oferece a atualização.

A implementação antiga em PowerShell + WPF (1.2 e anteriores) fica na branch [`legacy`](https://github.com/lippdev/consolemode/tree/legacy) e não é usada pelo app WinUI.

