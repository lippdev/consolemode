# Console Mode: PRD (documento de requisitos do produto)

Documento vivo. Registra o que o produto é, o que já foi entregue desde o início, o que falta para a 1.7.0
e para onde queremos ir. As decisões de escopo ficam aqui; o detalhe de cada mudança fica no
[CHANGELOG](../CHANGELOG.md), e a validação manual em [TESTING.md](TESTING.md).

Última revisão: 29/10/2026 · Versão publicada: 1.7.0-alpha.2 · Alvo atual: 1.7.0

## 1. Visão

Transformar o PC do usuário em um console, sem esforço: um gesto (botão do controle, atalho ou link) e a TV vira
a tela de jogo, as outras telas apagam, o áudio muda, o launcher abre em tela cheia. Ao terminar, a mesa volta
exatamente como estava.

**Princípios**
1. **Controle primeiro.** Tudo precisa funcionar sem mouse e teclado, a 3 metros da TV.
2. **Reversível.** Nenhuma sessão deixa a mesa diferente do que encontrou (telas, áudio, ícones, ponteiro).
3. **Sem ferramentas de terceiros embutidas.** Telas e áudio usam as APIs do próprio Windows.
4. **Honesto sobre o que não dá para testar.** O que depende de hardware vai para a checklist manual, não para uma promessa.
5. **Gratuito e aberto** (AGPL-3.0), com pedidos e traduções vindos da comunidade.

## 2. Público

- **Jogador de sofá:** PC no quarto ou na sala, TV como tela de jogo, quer a experiência de um console.
- **Setup híbrido:** o mesmo PC é mesa de trabalho e console; precisa alternar e voltar sem refazer nada.
- **Entusiasta de HTPC e handheld:** usa Playnite, Steam Big Picture ou modo Xbox em tela cheia.
- **Colaborador:** traduz, reporta setups que funcionam ou quebram, abre PRs.

## 3. Histórico: como chegamos aqui

Primeiro commit em 07/07/2026. Primeira versão estável com a interface atual (1.3.0) em 23/09/2026.

| Versão | Data | Marco |
|---|---|---|
| Protótipo | 07/07 a 13/07 | App WinForms que chamava ferramentas externas para trocar de tela. |
| **1.3.0** | 23/09 | Interface nativa (WinUI 3) para escolher a tela de jogo e o que fazer com as outras; disposição pela posição real; navegação por controle; resolução, taxa, áudio, HDR, VRR e limite de FPS; instalador por usuário e versão portátil; avisos de atualização. |
| **1.4.0** | 23/09 | Interface em inglês, com troca de idioma sem reiniciar. |
| **1.5.0** | 25/09 (11 betas) | **Interface Console** em tela cheia, pensada para controle (Xbox e PlayStation). **Menu da sessão** por cima do jogo (Select + Y). Atalhos de sofá (Home entra, Start + Select volta; DualSense e DualShock por HID). Automação: links `consolemode://`, `--stop` e API local por pipe nomeado (contribuição de @nextestudios). Tela "Testar controle", espanhol, botão de feedback. |
| 1.5.1 | 28/09 | Opção de receber versões de teste. |
| **1.6.0** | 06/10 (5 alphas, 8 betas) | Interface Console redesenhada (abas Início, Sessão e Sistema com LB/RB). Menu da sessão no estilo do overlay da Steam, com janelas abertas e troca entre elas. **Explorador de arquivos ControlFS**. Contador de FPS (RTSS). Atalhos de controle configuráveis. **Controle da TV** (Google TV / Android TV: liga, troca a entrada HDMI, standby ao voltar). Fundo com capas da Steam e sons da interface. **Telas e áudio passaram a usar as APIs do Windows** (sem MultiMonitorTool nem SoundVolumeView). Correções de restauração (retrato, tela principal, Playnite lento). Publicação automática no winget. |
| 1.6.1 | 10/10 | Tour guiado do menu da sessão; contador de FPS reformulado (cartão, tamanhos, posição, estilos); esconder o ponteiro e controlar o mouse pelo controle. |
| 1.6.2 | 17/10 | Minimizar para a bandeja; interface em alemão, francês e russo (alemão por u/Skinner99). |
| Pós-1.6.2 | até 19/10 | Teste que reprova tradução copiada em inglês; README e guia em seis idiomas; resumo da atualização localizado; **Diagnóstico** com relatório copiável; **bateria do controle**; **modo Aplicativo personalizado** (qualquer `.exe`). |
| **1.7.0-alpha.1** | 19/10 | Interface Console no estilo de console doméstico: lançadores em fileira na Início, Jogar agora em pílula, Sistema com categorias à esquerda; fonte Inter; o foco escolhe o lançador. |
| **1.7.0-alpha.2** | 19/10 | Fundo padrão **Aurora**; a lista de ajustes não passa mais da borda da tela; hover e clique suaves nos botões da interface Console. |

Seis idiomas (pt-BR, en-US, es-ES, de-DE, fr-FR, ru-RU) e mais de 170 PRs e issues até aqui.

## 4. Estado atual (1.7.0 em preparação)

### 4.1 Já na `main` e nas alphas
Interface Console no novo estilo, fonte Inter, modo Aplicativo personalizado, bateria do controle, Diagnóstico,
fundo Aurora, hover e clique suaves (#159 a #166).

### 4.2 Abertos, com CI verde e sem conflito
| PR | Recurso | Pendência |
|---|---|---|
| #167 | Devolver os ícones da área de trabalho ao lugar depois da sessão (issue #30) | Validar no hardware. |
| #168 | Controle acorda o PC do repouso | Tratar o caso "nenhum dispositivo encontrado" (o script termina com sucesso sem ter mudado nada). Validar no hardware. |
| #169 | Logos oficiais dos lançadores nos tiles da Início | Validar com lançadores instalados e não instalados. |
| #170 | Perfis por jogo: resolução, HDR, FPS e áudio (issue #69) | Validar com jogo da Steam aberto direto e via Playnite ou Xbox. |
| #171 | Protótipo HTML da interface Console | Só documentação. |

### 4.3 Em discussão
- **#173** (contribuidor externo): scripts próprios de início e fim, taxa de atualização exata (119,88 Hz) e ponteiro estacionado. Pedimos a divisão em três PRs.
  - **Ponteiro** (issue #172): entra primeiro, **ligado por padrão** e devolvendo a posição ao voltar.
  - **Scripts:** guardar o comando do fim em disco, como o backup de áudio.
  - **Taxa NTSC:** exige teste em TV real.

### 4.4 Critérios de saída da 1.7.0
1. Os PRs de 4.2 mesclados e a checklist de `TESTING.md` de cada um validada no hardware.
2. Ponteiro estacionado (#172) decidido: entra na 1.7.0 ou na 1.7.1.
3. `[Unreleased]` movido para `## [1.7.0]` nos dois changelogs; README e GUIDE (e as traduções enxutas) atualizados; roadmap do README revisado.
4. Fork do winget sincronizado antes da estável.
5. Tag e release **só** com autorização explícita do dono.

## 5. Roadmap

Prioridade: **P1** próxima versão, **P2** depois, **P3** explorar. Tamanho: **S** dias, **M** 1 a 2 semanas, **L** mais de 2 semanas, **XL** projeto próprio. Estimativas são palpites, não compromissos.

### 5.1 Integrações de lançadores e apps
| Item | Prioridade | Tam. | Descrição e critério de aceite |
|---|---|---|---|
| **JoyChromium** | P1 | L | A web com controle, direto do Console Mode: tile na Início, abre na tela de jogo, volta ao PC ao fechar. Critério: navegar, clicar e digitar só com o controle; sessão restaurada ao sair. |
| **Tako Launcher** | P2 | M | Integração completa: aparecer como lançador na Início, abrir em tela cheia na tela de jogo, restaurar ao fechar, logo oficial. *A definir: como detectar a instalação, se há modo tela cheia e qual processo vigiar.* |
| **Hydra Launcher** | P2 | M | Mesmo contrato do item acima. *A definir: caminho de instalação e processo real; launchers que só iniciam outro programa e saem pedem o "Processo a vigiar" do modo personalizado.* |
| **AnyFSE** | P2 | M | Integrar para que o modo Xbox em tela cheia abra o launcher que o usuário escolher. *A definir: onde o Console Mode entra (detectar, configurar ou só conviver) e como fica a restauração.* |
| **Jogos nativos, sem launcher externo** | P3 | XL | Biblioteca própria: listar jogos instalados (Steam, Epic, GOG, Xbox, pastas), abrir direto pela interface Console, com capas e perfis por jogo (#170). Critério: iniciar e acompanhar um jogo sem abrir nenhum launcher visível. É o maior salto de escopo do produto: decidir antes se queremos ser launcher ou orquestrador. |

### 5.2 Apoio ao projeto
| Item | Prioridade | Tam. | Descrição |
|---|---|---|---|
| **BuyMeACoffee ou Ko-fi** | P1 | S | Link de apoio em Ajustes, no README e no `FUNDING.yml` do GitHub, sem pop-up e sem bloquear recurso. Escolher a plataforma (conferir taxas e condições atuais antes). Texto nos seis idiomas. |

### 5.3 Experiência do usuário
| Item | Prioridade | Tam. | Descrição |
|---|---|---|---|
| Desfazer seguro após falha | P1 | M | Persistir em disco tudo o que a sessão mudou (comandos de fim, ponteiro, ícones), para restaurar no próximo início se o app cair. |
| Primeira execução guiada | P1 | M | Assistente curto: escolher a tela de jogo, testar, configurar o atalho do controle. Hoje o setup mostra só os atalhos. |
| Pré-checagem antes de começar | P2 | S | TV ligada, entrada certa, launcher instalado. Aponta o problema em vez de falhar no meio da sessão. |
| Perfis de setup | P2 | M | Vários setups salvos (sala, quarto, simulador) e escolha ao entrar; base para o #112 (várias telas de jogo). |
| Acessibilidade | P2 | M | Leitor de tela nas interfaces, contraste alto (já há tokens no menu da sessão), tamanho da fonte. |
| Polimento da interface Console | contínuo | M | Hover e foco consistentes, legibilidade em TV com sobrevarredura, escala do texto, legibilidade no tema claro do menu da sessão. |
| Revisão das traduções | contínuo | S | Revisão humana do francês e do russo (traduzidos por máquina). |

### 5.4 Plataforma e distribuição
| Item | Prioridade | Tam. | Descrição |
|---|---|---|---|
| Assinatura de código | P1 | M | Reduz o aviso do SmartScreen e falsos positivos de antivírus (ver `docs/CODE_SIGNING.md`). |
| Outros canais | P2 | S | O winget já existe; avaliar Scoop e Chocolatey. |
| **Linux e macOS** (#74) | P3 | XL | Hoje o app é WinUI 3, só Windows. Exige separar o núcleo (lógica de sessão, perfis, API de controle) da camada de sistema e da interface, e escolher uma interface multiplataforma. Primeiro passo: extrair o núcleo para uma biblioteca testável. |

### 5.5 Outras ideias
| Item | Prioridade | Tam. | Descrição |
|---|---|---|---|
| Gravar os últimos 30 s (#77) | P2 | M | Pelo menu da sessão, via NVIDIA Instant Replay / Highlights. |
| Controle remoto pelo celular (#72) | P2 | M | Usar a API local de controle por uma página no celular. |
| WakeOn (#71) | P2 | M | Acordar o PC já entrando no modo console, junto do app irmão. Complementa o wake pelo controle (#168). |
| Várias telas de jogo (#112) | P2 | L | Simulador de corrida e de voo (três telas). |
| HDMI-CEC (#75) | P3 | L | Ligar e desligar a TV e trocar a entrada onde a TV não tem rede. Precisa de adaptador USB-CEC. |
| Gatilhos de voz e assistente (#12) | P3 | M | Alexa e MCP para entrar no modo console. |
| Widget da Xbox Game Bar (#11) | P3 | M | Iniciar e restaurar pela Game Bar. |
| Compartilhar setups (#15) | contínuo | S | Coletar combinações de TV e monitor que funcionam ou quebram; alimenta o Diagnóstico. |
| Telemetria opcional e anônima | P3 | M | Só se vier **desligada por padrão** e documentada; ajudaria a ver falhas de restauração. Decisão de privacidade do dono. |

## 6. Requisitos transversais

- **Strings:** todo texto novo nos seis catálogos, traduzido; os testes reprovam frase em inglês copiada.
- **Documentação:** README e GUIDE em par (pt-BR e en); as traduções enxutas acompanham as seções que têm.
- **Testes:** lógica pura em arquivos ligados ao projeto de testes; o que depende de tela, TV, áudio ou controle vai para `TESTING.md`.
- **Fluxo:** um assunto por PR, Conventional Commits em inglês, número do PR no fim do item do changelog.
- **Segurança:** o app só lê o que o Windows e os lançadores já expõem; instaladores baixados são verificados por SHA-256; segredos nunca vão para o relatório de diagnóstico.
- **Release:** tag e publicação só com pedido explícito do dono.

## 7. Riscos e questões abertas

| Risco ou questão | Impacto | Como tratar |
|---|---|---|
| Restauração falha depois de uma queda do app | Mesa fica trocada | Persistir tudo em disco (5.3, "Desfazer seguro"). |
| Crescimento de escopo (launcher próprio, multiplataforma) | Dilui o foco e trava as versões | Decidir "orquestrador ou launcher" antes de começar 5.1 (jogos nativos) e 5.4 (Linux e macOS). |
| Dependência de terceiros (Tako, Hydra, AnyFSE, JoyChromium) | Quebra quando eles mudam | Detecção tolerante, logos e caminhos lidos do que está instalado, teste manual a cada versão. |
| Só um mantenedor testa no hardware | Gargalo de validação | Checklists curtas; pedir testes à comunidade (#15). |
| Aviso do SmartScreen e antivírus | Afasta usuários novos | Assinatura de código (5.4). |
| Contribuições grandes e misturadas (#173) | Revisão lenta | Pedir PRs pequenos; manter o `AGENTS.md` como guia. |

**Perguntas para o dono**
1. Ko-fi ou BuyMeACoffee? Já existe conta criada?
2. Tako Launcher, Hydra e AnyFSE: qual é o link oficial de cada um e o que precisam para abrir em tela cheia? (Não verifiquei os três; as linhas acima são pontos de partida.)
3. "Jogos nativos": queremos ser um launcher ou continuar orquestrando os que existem?
4. Linux e macOS entram em 2027 ou ficam como "explorando"?
5. O ponteiro estacionado (#172) entra na 1.7.0?
