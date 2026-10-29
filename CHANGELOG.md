# Histórico de alterações

Notas em português do Brasil; a versão em inglês (Estados Unidos) fica em `CHANGELOG.en-US.md`. Antes de publicar uma versão, adicione uma seção `## [VERSÃO]` com o changelog daquela versão **nos dois arquivos**. O workflow publica as duas seções correspondentes à tag na mesma release e falha se faltar alguma.

## [Unreleased]
### Novidades
- Os ícones da área de trabalho voltam ao lugar depois da sessão: trocar a tela principal podia fazer o Windows embaralhá-los. O Console Mode salva a posição antes de mexer nas telas e os recoloca alguns segundos depois de restaurar a mesa. Liga e desliga em Ajustes → **Devolver os ícones da área de trabalho ao lugar** (ligado por padrão); não faz nada com **Organizar ícones automaticamente** ligado. (#167)
- **Acordar o PC pelo controle**: em Ajustes (e na aba Sistema → Controle do modo Console), o Console Mode mostra se o adaptador sem fio Xbox, o Bluetooth ou o controle no cabo podem acordar o PC da suspensão e, em **Ativar**, liga isso para eles (o Windows pede permissão de administrador). Junto com **Entrar ao conectar um controle**, apertar o botão central do controle acorda o PC direto no modo console. Não funciona com o PC desligado ou em hibernação. (#168)
- Interface Console: os blocos do Início mostram os logos oficiais da Steam, do Playnite e do Xbox, lidos dos apps instalados, e o Aplicativo personalizado mostra o ícone do seu `.exe`. Sem o app instalado, o bloco mantém o ícone genérico. (#169)
- **Perfis por jogo**: cada jogo da Steam pode ter sua própria resolução, HDR, limite de FPS e saída de áudio. Com o jogo rodando, abra o menu da sessão e escolha **Perfil do jogo → Salvar os ajustes atuais**; da próxima vez que ele abrir no modo console, o perfil é aplicado sozinho e desfeito quando o jogo fecha. Vale também para jogos da Steam abertos pelo Playnite ou pelo app Xbox. Os perfis ficam em Ajustes → **Perfis por jogo**, onde dá para apagá-los. (#170)
- **Navegador (JoyChromium)** no menu da sessão, fixo logo abaixo do ControlFS e funcionando do mesmo jeito: o [JoyChromium](https://github.com/lippdev/joychromium) (o navegador para a TV que se usa com o controle) abre em tela cheia por cima do jogo e, se não estiver instalado, o primeiro clique baixa a versão mais recente, confere o SHA-256 e instala. Voltar ao PC com ele aberto o tira da tela cheia e o deixa como janela, sem perder as abas. (#175, #181)
- **Tirar o ponteiro do mouse do caminho** (issue #172): quando o iniciador aparece na TV, o ponteiro vai para o canto inferior direito da tela de jogo (duas vezes, porque a Steam o recentraliza ao abrir) e, ao voltar ao PC, volta para onde estava. Ligado por padrão; desligue em Ajustes ou em Sistema → Ao entrar no modo console. Baseado na versão de @JarJarShinks no #173. (#177)

### Correções
- Aplicativo personalizado: o Console Mode leva para a tela de jogo só a maior janela do programa, em vez de todas as que o Windows aponta como principal; uma dica de ferramenta não é mais esticada sobre a TV. (#175)
- Se o app fechar ou o PC desligar no meio de uma sessão, a próxima abertura do Console Mode restaura tudo o que a sessão mudou (telas, áudio, RTSS, ícones da área de trabalho e ponteiro) e avisa. Antes, as telas ficavam como a sessão deixou até você arrumar à mão. (#178)
- Saída de áudio **A que aparecer ao conectar**: quando a saída da própria TV já estava ligada antes da sessão (e por isso nunca "aparecia"), o áudio ficava na mesa; agora, se nenhuma saída nova surgir em uns 10 s, o Console Mode usa a saída da tela de jogo que já estava ativa. Uma soundbar ou receiver que acorde depois continua tendo prioridade. (issue #149, #180)
- Controles: um analógico que aparece fora do centro desde o início (um eixo que não é analógico de verdade, ou uma leitura errada) não deixa mais o menu "subindo sozinho". O analógico só passa a valer depois de ser visto no centro; o direcional e os botões continuam funcionando. (issue #149, #180)
- Interface Console, aba Sessão: as legendas dos ajustes rápidos quebram em duas linhas em vez de serem cortadas com reticências ("Resolución y frecuencia…" em espanhol, por exemplo). (#180)

## [1.7.0-alpha.2]
### Novidades
- Interface Console: plano de fundo novo por padrão, a **Aurora**, com brilhos suaves verde e azul e linhas finas em arco sobre um azul bem escuro. As capas da Steam continuam disponíveis em Sistema → Plano de fundo → Capas da Steam (quem usava o fundo padrão passa a ver a Aurora). (#165)

### Correções
- Configurações do modo Console: a lista de opções da aba Sistema não passa mais da borda direita da tela e começa logo ao lado das categorias, e o fundo escurece nessa aba para as linhas ficarem legíveis sobre qualquer capa ou imagem. Clicar com o mouse numa categoria agora sempre a abre; antes, depois de entrar nas opções, o clique voltava para a categoria anterior. (#165)
- Configurações do modo Console: botões das categorias e opções sem o cinza e o texto apagado do estilo padrão; passar o mouse e clicar agora têm transição suave, e a categoria escolhida acende com animação. (#165)

## [1.7.0-alpha.1]
### Novidades
- Interface Console com cara de console de mesa: o Início mostra os iniciadores numa fileira no topo, sobre a arte dos seus jogos (Jogar agora em formato de pílula logo abaixo, com a tela, o áudio, a resolução e o HDR numa linha só), as abas ficam à esquerda e a aba Sistema virou uma lista de categorias à esquerda com só as opções daquela categoria à direita. Blocos sem borda fina, cantos mais arredondados e menos repetição: o resumo do setup saiu do Início (está na Sessão). (#162)
- Interface Console: fonte nova, a Inter (embutida no app), com o título e o Jogar agora em negrito, mais legível de longe na TV. No Início, andar pela fileira de iniciadores já escolhe o que abre, como no PS5: o escolhido ganha um selo ✓ e uma barra verde, os outros ficam mais apagados, e A no bloco abre direto. (#164)
- Modo **Aplicativo personalizado**: em Ajustes → Ao entrar no modo console, escolha qualquer `.exe` (RetroArch, outro launcher…) com argumentos opcionais. O Console Mode prepara a TV, abre o app na tela de jogo e restaura a mesa sozinho quando ele fecha. Para launchers que só abrem outro programa e fecham, informe em **Processo a acompanhar** o nome do processo do programa de verdade. Se ele nunca aparecer, a sessão continua até você encerrá-la (segurar Start + Select). (#161)
- Bateria do controle: o cabeçalho do menu da sessão e a barra de cima da interface Console (ao lado do relógio) mostram o nível da bateria do controle, com o ícone de carregando quando ele está no cabo. Funciona com controles Xbox sem fio, DualSense e DualShock 4 (por USB e Bluetooth); quando o Windows só dá um nível aproximado, o número vem com "~". Com vários controles, aparece o mais descarregado, e controles com fio sem bateria não mostram nada. Durante a sessão, um aviso no canto da TV avisa quando a bateria chega a 15 % e de novo a 5 %, sem repetir. (#160)
- Diagnóstico em Ajustes: um card novo mostra, só para leitura, a versão e o canal do app, o Windows, as telas (resolução, HDR, qual é a de jogo e quais são ocultadas), as saídas de áudio, os controles conectados, se Steam, Playnite, RTSS e ControlFS foram encontrados e o fim do log. **Copiar relatório** copia tudo em texto para colar numa issue, sem o seu nome de usuário nos caminhos e sem chaves. (#159)
- Aviso de atualização em português: o destaque da nova versão aparece no idioma do app quando há um changelog nele (hoje, português); os outros idiomas continuam vendo o destaque em inglês. Vale a partir das atualizações publicadas depois desta versão. (#158)

## [1.6.2]
### Novidades
- Minimizar para a bandeja: com a nova opção em Ajustes (também na interface Console, em Sistema), minimizar a janela tira o Console Mode da barra de tarefas e deixa só o ícone nos ícones ocultos; um clique no ícone abre de novo. Vem desligada. (#150)
- Interface em alemão (Deutsch), traduzida por [u/Skinner99](https://www.reddit.com/user/Skinner99/) no Reddit, e em francês (Français) e russo (Русский). Quem usa o Windows num desses idiomas já abre o app nele, e o instalador também oferece as opções. (#151)

## [1.6.1]
### Novidades
- Tour guiado do menu da sessão: na primeira vez que o menu abre, ele oferece um tour opcional que destaca cada opção e explica o que ela faz, incluindo a diferença entre **Voltar ao PC** (restaura a mesa e o app fica na bandeja) e **Sair do Console Mode** (restaura e fecha o app). Para ver de novo, aperte Y (△) ou F1 no menu, ou use Ajustes → Tour do menu da sessão. (#144)
- Contador de FPS repaginado: os números ficam num card escuro e arredondado (dá para desligar) em três tamanhos, Pequeno, Médio e Grande. Dá para escolher o canto da tela onde ele aparece (ou deixar a posição do RTSS) e ajustar tudo pelo próprio menu da sessão: A em "Contador de FPS" abre a lista com estilo, tamanho, posição, fundo, cor e, no Personalizado, os itens. Novos estilos: Apenas FPS, Compacto (FPS e uso de CPU, GPU e RAM numa linha) e Detalhado (FPS, tempo de quadro e API, mais clock da CPU, clock e temperatura da GPU, VRAM e RAM). No Personalizado, os itens do PC também podem ser escolhidos um a um. Os dados vêm do Windows e do driver de vídeo, sem instalar nada; quem já usava "Compacto" continua vendo só o FPS. (#146)
- Cursor do mouse fora do caminho: a linha "Ocultar o cursor do mouse" do menu da sessão esconde o cursor (útil em emuladores que deixam o cursor no meio da TV) até você desligá-la ou voltar ao PC; nada acontece sozinho. E, para jogos e emuladores cujos menus não aceitam o controle, a linha "Controlar o mouse com o controle" do menu da sessão faz o analógico esquerdo mover o cursor, o direito rolar, A clicar, A duas vezes clicar com o botão direito e A segurado arrastar. Vale até o fim da sessão. (#145)

### Mudanças
- O menu da sessão não mostra mais a linha "Gravar os últimos 30 s · em breve", que nunca funcionou. (#147)

### Atualizando da 1.6.0
- O app mostra o aviso: clique em **Atualizar agora**. As configurações são mantidas; quem usava o contador "Compacto" continua vendo só o FPS (agora com o nome "Apenas FPS").

## [1.6.0]
### Novidades
- Interface Console redesenhada, com cara de console: barra no topo com três abas (Início, Sessão e Sistema) que você troca com LB/RB (L1/R1 no PlayStation), blocos das telas que mostram o que vai acontecer com cada monitor, ajustes rápidos com ícones e foco que cresce ao passar. Início traz o banner com Jogar agora e um resumo do setup atual (tela de jogo, áudio, iniciador e HDR/VRR). (#117)
- Menu da sessão redesenhado como o overlay do Steam (Shift+Tab): cobre a tela inteira, o jogo escurece e desfoca por trás, um painel lateral reúne as opções (volume com medidor, resolução, áudio, FPS, HDR) e no meio ficam as **janelas abertas**, um Alt+Tab interno: o direcional escolhe, A traz a janela para a frente, X a fecha (com confirmação) e B volta ao jogo. Os cartões mostram o ícone de cada programa. Fora da sessão ele abre como prévia. (#120, #127, #133, #135)
- Explorador de arquivos no menu da sessão: a linha "Explorador de arquivos (ControlFS)" abre o ControlFS, o gerenciador de arquivos de código aberto da equipe, feito para o controle, em tela cheia na tela da sessão. Se ele não estiver instalado, A baixa e instala ali mesmo, com o instalador conferido (SHA-256) antes de rodar. (#122, #136, #137)
- Contador de FPS sobre o jogo, pelo RivaTuner (RTSS): escolha em Ajustes ou alterne com A na linha "Contador de FPS" do menu da sessão. Estilos: Afterburner (padrão; não mexe em nada), Desligado, Compacto (só o FPS), Detalhado (API, FPS e tempo de quadro) e Personalizado, em que você escolhe os itens, se ficam numa linha só e a cor dos números. Fora de "Afterburner", o OSD do MSI Afterburner fica escondido durante a sessão e volta sozinho ao voltar ao PC. (#138, #139)
- Atalhos do controle à sua escolha: o botão que abre o Console Mode, o do menu da sessão e o de voltar ao PC são configuráveis em Ajustes (segure os botões que você quer e solte). Nenhum vem ligado: na primeira abertura o app mostra a configuração, com uma sugestão para cada ação (Home, Select + Y e Start + Select). Um mesmo botão não pode servir para duas ações. (#116)
- Ajustes → TV: o modo console liga a Google TV / Android TV pela rede e troca para a entrada HDMI do PC; também pode colocá-la em espera ao voltar ao PC. (#126)
- Plano de fundo da interface Console: por padrão, uma colagem com as capas dos seus jogos instalados na Steam sob um gradiente escuro (o app só lê as imagens que a Steam já baixou). Dá para trocar por só o gradiente ou por uma imagem sua, em Sistema → Plano de fundo. (#117)
- Sons na interface Console ao mover o foco, escolher e voltar, criados pelo próprio app. Dá para desligar em Ajustes → Sons da interface. (#117)
- Interface Console: a aba Sistema dá acesso aos ajustes do modo PC (Playnite, TV, contador de FPS, Steam, tutorial e pasta de dados), com as opções de controle agrupadas, a escolha do layout dos botões (Automático, Xbox ou PlayStation) com prévia e um editor de atalhos com cartões e botões maiores. (#128, #131)

### Mudanças
- Telas e áudio agora são controlados pelas APIs do próprio Windows. O app não embute mais o MultiMonitorTool nem o SoundVolumeView, da NirSoft: o pacote fica só com código do projeto e o rtss-cli (MIT), o que abre caminho para assinar o executável e reduz alertas falsos de antivírus. As telas salvas, a saída de áudio escolhida e o backup da mesa continuam valendo. (#91, #97, #98, #99, #110, #119)
- Steam: ao voltar ao PC, o Big Picture é fechado e a janela da Steam vai para a bandeja, com o cliente ainda em execução. O pedido só vai para janelas da própria Steam, então jogos e outras janelas não são tocados, e nada acontece se houver um jogo rodando. Dá para desligar em Ajustes → "Mandar a Steam para a bandeja ao voltar ao PC". (#121, #131, #140)
- Publicar uma versão estável também abre o PR de atualização no winget (`winget install lippdev.ConsoleMode`). (#106)

### Correções
- Telas: ao voltar para o PC o layout volta idêntico ao de antes (a tela 2 não desliza mais para o lado) e a TV continua desligada. O resultado é conferido com o backup e refeito se preciso. (#134)
- Telas: um monitor girado (em retrato) volta em retrato ao restaurar a mesa; antes a rotação não era salva. (#108)
- Telas: a tela principal vem da flag do próprio Windows, então com telas clonadas não há mais ambiguidade. (#110)
- Playnite: quando ele demora para abrir, o app não volta mais para o PC depois de alguns segundos; só restaura a mesa quando o Playnite fecha de verdade. (#83)
- Áudio: se a troca da saída falhar, a sessão continua em vez de ser desfeita depois de as telas já terem sido trocadas. (#109)
- Controles: o DualSense (PS5) por Bluetooth e o DualShock são lidos direto por HID nos atalhos, na interface Console e no menu da sessão. (#130, #132)
- Controles: um controle que manda leituras impossíveis (muitos botões apertados ao mesmo tempo, direcional preso) é ignorado. (#115)
- Interface Console: navegar pelo controle não trava mais; "para cima" e "para baixo" funcionam entre colunas e, ao trocar de aba, o foco cai no primeiro item dela. (#117)
- Menu da sessão: o aviso que mostra o atalho no início da sessão não fica mais preso na tela. (#114)

### Atualizando da 1.5.1
- O app mostra o aviso: clique em **Atualizar agora**. As configurações, as telas salvas e a saída de áudio são mantidas.
- Os atalhos do controle precisam ser escolhidos de novo: quem usava o botão Home ou Start + Select confirma (ou troca) na configuração que o app mostra na primeira abertura.
- Os arquivos antigos do MultiMonitorTool e do SoundVolumeView ficam na pasta `tools` até você apagá-los à mão; o app não os usa mais.

## [1.6.0-beta.8]
### Atenção: versão beta, não é a estável
- Esta é uma versão de teste da 1.6. A versão estável continua sendo a 1.5.1, e quem está nela não recebe esta atualização automaticamente: só quem ligou Ajustes → "Receber versões de teste (alpha e beta)" ou já está numa alpha/beta. Se algo falhar, volte para a 1.5.1 e conte pelo botão de feedback (anexe o `consolemode.log`).

### Correções
- Contador de FPS: o estilo salvo em Ajustes agora vale assim que a sessão começa. Antes, o contador só aparecia depois de alternar a linha "Contador de FPS" no menu da sessão. (#139)
- Voltar ao PC: o pedido de fechar o Big Picture só vai para janelas da própria Steam. Antes, um jogo em tela cheia (os feitos com SDL, como os da Valve e muitos indies) podia receber o pedido e fechar. (#140)

## [1.6.0-beta.7]
### Atenção: versão beta, não é a estável
- Esta é uma versão de teste da 1.6. A versão estável continua sendo a 1.5.1, e quem está nela não recebe esta atualização automaticamente: só quem ligou Ajustes → "Receber versões de teste (alpha e beta)" ou já está numa alpha/beta. Se algo falhar, volte para a 1.5.1 e conte pelo botão de feedback (anexe o `consolemode.log`).

### Novidades
- Contador de FPS sobre o jogo, pelo RivaTuner (RTSS), no estilo do Console Mode: escolha em Ajustes ou alterne com A na nova linha "Contador de FPS" do menu da sessão. Estilos: Afterburner (padrão; não mexe em nada), Desligado (nada na tela), Compacto (só o FPS), Detalhado (API, FPS e tempo de quadro) e Personalizado, em que você escolhe os itens, se ficam numa linha só e a cor dos números. Fora de "Afterburner", o OSD do MSI Afterburner fica escondido durante a sessão e volta sozinho ao voltar ao PC; a configuração do Afterburner não é alterada. (#138)

## [1.6.0-beta.6]
### Atenção: versão beta, não é a estável
- Esta é uma versão de teste da 1.6. A versão estável continua sendo a 1.5.1, e quem está nela não recebe esta atualização automaticamente: só quem ligou Ajustes → "Receber versões de teste (alpha e beta)" ou já está numa alpha/beta. Se algo falhar, volte para a 1.5.1 e conte pelo botão de feedback (anexe o `consolemode.log`).

### Correções
- Menu da sessão: depois de instalar o ControlFS pela linha "Explorador de arquivos", o menu continua na frente e a linha diz "Instalado · aperte para abrir"; o ControlFS abre no próximo A. Antes ele abria sozinho atrás do jogo e o menu sumia. (#137)

## [1.6.0-beta.5]
### Atenção: versão beta, não é a estável
- Esta é uma versão de teste da 1.6. A versão estável continua sendo a 1.5.1, e quem está nela não recebe esta atualização automaticamente: só quem ligou Ajustes → "Receber versões de teste (alpha e beta)" ou já está numa alpha/beta. Se algo falhar, volte para a 1.5.1 e conte pelo botão de feedback (anexe o `consolemode.log`).

### Novidades
- Menu da sessão: sem o ControlFS instalado, apertar A em "Explorador de arquivos (ControlFS)" agora baixa e instala o ControlFS ali mesmo, sem sair do controle: a linha mostra o progresso, o instalador é conferido (SHA-256) antes de rodar e, ao terminar, o ControlFS abre em tela cheia. Se não der (sem internet, por exemplo), a linha avisa e o próximo A abre a página de download. (#136)

## [1.6.0-beta.4]
### Atenção: versão beta, não é a estável
- Esta é uma versão de teste da 1.6. A versão estável continua sendo a 1.5.1, e quem está nela não recebe esta atualização automaticamente: só quem ligou Ajustes → "Receber versões de teste (alpha e beta)" ou já está numa alpha/beta. Se algo falhar, volte para a 1.5.1 e conte pelo botão de feedback (anexe o `consolemode.log`).

### Correções
- Menu da sessão: os cartões das janelas têm todos o mesmo tamanho e mostram o ícone certo de cada programa (também de apps da Microsoft Store), nítido na TV; a confirmação de fechar janela ganhou um cartão próprio com o ícone e o título da janela, "Cancelar" e "Fechar janela" em destaque. (#135)

## [1.6.0-beta.3]
### Atenção: versão beta, não é a estável
- Esta é uma versão de teste da 1.6. A versão estável continua sendo a 1.5.1, e quem está nela não recebe esta atualização automaticamente: só quem ligou Ajustes → "Receber versões de teste (alpha e beta)" ou já está numa alpha/beta. Se algo falhar, volte para a 1.5.1 e conte pelo botão de feedback (anexe o `consolemode.log`).

### Correções
- Voltar do Big Picture agora fecha apenas a janela da Steam para a bandeja, mantendo o cliente em execução; jogos e outras janelas são preservados. (#131)
- Ajustes da interface Console: opções de controle agrupadas, escolha persistente do layout Automático/Xbox/PlayStation com prévia e editor de atalhos com cartões e botões maiores. (#131)

## [1.6.0-beta.2]
### Atenção: versão beta, não é a estável
- Esta é uma versão de teste da 1.6. A versão estável continua sendo a 1.5.1, e quem está nela não recebe esta atualização automaticamente: só quem ligou Ajustes → "Receber versões de teste (alpha e beta)" ou já está numa alpha/beta. Se algo falhar, volte para a 1.5.1 e conte pelo botão de feedback (anexe o `consolemode.log`).

### Correções
- Menu da sessão: ← e → percorrem os cartões das janelas abertas pela posição, sem falhar; no último cartão da linha nada se move. (#133)
- Controles: o DualSense (PS5) por Bluetooth voltou a ser lido pelas teclas de atalho e pelo menu da sessão; o log registra o formato do primeiro relatório de cada controle Sony. (#132)

## [1.6.0-beta.1]
### Atenção: versão beta, não é a estável
- Esta é uma versão de teste da 1.6. A versão estável continua sendo a 1.5.1, e quem está nela não recebe esta atualização automaticamente: só quem ligou Ajustes → "Receber versões de teste (alpha e beta)" ou já está numa alpha. Se algo falhar, volte para a 1.5.1 e conte pelo botão de feedback (anexe o `consolemode.log`).

### Correções
- Telas: ao voltar para o PC o layout volta idêntico ao de antes (a tela 2 não desliza mais para o lado) e a TV continua desligada. Só voltam as telas que estavam ligadas, a TV é desconectada antes das posições, o resultado é conferido com o backup e refeito se preciso. (#134)

## [1.6.0-alpha.5]

### Correções
- Tela Console: leitura direta do DualShock por HID; overlay: navegação da lateral para as janelas, remoção do X dos cartões e logo do ControlFS. (#130)


## [1.6.0-alpha.4]
### Novidades
- Ajustes → TV: o modo console liga a Google TV / Android TV pela rede e troca para a entrada HDMI do PC; também pode colocá-la em espera ao restaurar. (#126)

### Correções
- Interface Console: a aba Sistema agora permite configurar atalhos personalizados e acessar os ajustes do modo PC, incluindo Playnite, Android TV, FPS personalizado, fechar a Steam, tutorial e pasta de dados. (#128)
- Menu da sessão: fechar uma janela pelo Alt+Tab interno agora pede confirmação; Cancelar mantém a janela aberta e devolve o foco ao cartão. (#127)

## [1.6.0-alpha.3]
### Novidades
- Menu da sessão: painel fixo no topo da lateral com o **Explorador de arquivos (ControlFS)**, o gerenciador de arquivos de código aberto da equipe, feito para o controle. Um toque abre o ControlFS por cima, sempre em tela cheia na tela da sessão (ou o traz para a frente se já estiver aberto); se não estiver instalado, abre a página de download. O Console Mode só o chama pelo `--start` dele, não o encerra nem envia F11 a outra janela se o Windows negar o foco. (#122)
- Menu da sessão (o atalho do controle, Select + Y na sugestão) redesenhado como o overlay do Steam (Shift+Tab): cobre a tela inteira, o jogo escurece e desfoca por trás, um painel lateral reúne as opções (volume com medidor, resolução, áudio, FPS, HDR) e no meio ficam sempre visíveis as **janelas abertas**, um ALT+TAB interno: o direcional escolhe, A traz a janela para a frente, X a fecha e B volta ao jogo (trocar de janela não encerra a sessão). O vidro fosco só aparece se os efeitos de transparência do Windows estiverem ligados (senão o painel fica escuro e sólido, e muda ao vivo se você trocar a configuração), e em alto contraste usa as cores do sistema. Tem foco que cresce, sons e animações (o painel entra deslizando, as linhas vêm em cascata e ao fechar ele some suavemente). Nesta alpha ele abre também **fora da sessão**, como prévia com um selo no topo, para testar sem ligar o modo console (nela o limite de FPS não aparece e "Voltar ao PC" vira "Fechar menu"). (#120)
- Interface Console redesenhada, com cara de console: barra no topo com três abas (Início, Sessão e Sistema) que você troca com LB/RB (L1/R1 no PlayStation) com uma animação de deslize, blocos das telas que mostram o que vai acontecer com cada monitor, ajustes rápidos com ícones e foco que cresce ao passar. Início traz o banner com Jogar agora e um resumo do setup atual (tela de jogo, áudio, iniciador e HDR/VRR), que abre cada escolha. (#117)
- Plano de fundo do modo Console: por padrão, uma colagem com as capas dos seus jogos instalados na Steam sob um gradiente escuro (o app só lê as imagens que a Steam já baixou; nada é embutido). Dá para trocar por só o gradiente ou por uma imagem sua, em Sistema → Plano de fundo. (#117)
- Sons na interface Console ao mover o foco, escolher e voltar (sons criados pelo próprio app, em volume baixo). Dá para desligar em Ajustes → Sons da interface. (#117)
- Atalhos do controle à sua escolha: o botão que abre o Console Mode, o do menu da sessão e o de voltar ao PC agora são configuráveis em Ajustes (basta segurar os botões que você quer e soltar no mesmo controle). A captura não mistura botões de controles diferentes. Nenhum vem ligado: na primeira abertura desta versão o app mostra a configuração, com uma sugestão para cada ação (Home, Select + Y e Start + Select). Um mesmo botão não pode servir para duas ações. Quem usava o botão Home ou Start + Select precisa escolher de novo. (#116)
- Ajustes → "Receber versões de teste (alpha e beta)": qualquer pessoa pode entrar nos testes das próximas versões pelo próprio app. Desligado por padrão; ao ligar, o app avisa que a versão pode ter bugs.

### Correções
- Menu da sessão: se o Windows não conseguir ativar a janela escolhida, o menu continua aberto para tentar de novo. (#120)
- Steam: ao voltar ao PC (botão, menu, atalho, link `stop` ou saindo do Big Picture) o Big Picture é sempre fechado e a Steam é chamada para fechar do jeito normal, o "Sair" dela, sem encerrar o processo à força. Antes ela ficava aberta e costumava ficar bugada. Não fecha a Steam se houver um jogo rodando, e dá para desligar em Ajustes → "Fechar a Steam ao voltar ao PC". (#121)
- Menu da sessão: o aviso que mostra o atalho no início da sessão não fica mais preso na tela; ele some sozinho depois de alguns segundos. (#114)
- Controles: um controle que manda leituras impossíveis pelo driver HID genérico (muitos botões apertados ao mesmo tempo, direcional preso) agora é ignorado também quando o Windows o expõe por Windows.Gaming.Input. O log avisa, e reabrir o app tenta de novo. (#115)
- Interface Console: navegar pelo controle não trava mais. "Para cima" (e "para baixo") funciona também quando o bloco de destino não está na mesma coluna, o foco não para mais em contêineres invisíveis e, ao trocar de aba, ele cai no primeiro item dela. (#117)
- Telas: um monitor girado (em retrato) mantém a rotação no backup do layout, então restaurar a mesa devolve o monitor em retrato. Antes a rotação nunca era salva nem restaurada. (#108)
- Playnite: quando ele demora para abrir, o app não volta mais para o PC depois de alguns segundos. Se o Playnite troca a janela de carregamento pela principal, o Console Mode passa a acompanhar a nova e só restaura a mesa quando o Playnite fecha de verdade. (#83)
- Áudio: se a troca da saída falhar, a sessão continua em vez de ser desfeita depois de as telas já terem sido trocadas. (#109)
- Telas: a tela principal vem da flag do próprio Windows, então com telas clonadas não há mais ambiguidade. (#110)

### Por trás dos panos
- Publicar uma versão estável agora também abre o PR de atualização no winget (`winget install lippdev.ConsoleMode`). Versões de teste ficam de fora. (#106)
- O código não cita mais o MultiMonitorTool nem o SoundVolumeView, e a limpeza dos arquivos antigos deles na pasta `tools` saiu: quem atualiza direto da 1.5 mantém esses dois arquivos até apagá-los à mão. (#119)
- O pacote embute só o rtss-cli; sobras de MultiMonitorTool/SoundVolumeView não entram mais no executável. (#110)

## [1.6.0-alpha.2]
### Mudanças
- A 1.6 é só código do projeto: saiu também a opção de voltar ao MultiMonitorTool / SoundVolumeView, e os arquivos antigos deles na pasta `tools` são apagados ao abrir o app. (#91)
- Telas e áudio são controlados pelas APIs do próprio Windows desde a 1.6.0-alpha.1; veja as notas dela abaixo. (#97, #98, #99)

### Atenção: versão alpha
- Ainda não validada em muitos setups de monitores e TV. Se algo falhar, volte para a 1.5.0 e conte pelo botão de feedback (anexe o `consolemode.log`). Quem usa a 1.5.0 estável não recebe esta versão automaticamente.

## [1.6.0-alpha.1]
### Novidades
- Telas e áudio agora são controlados pelas APIs do próprio Windows. O app não embute mais o MultiMonitorTool nem o SoundVolumeView, da NirSoft: o pacote fica só com código do projeto e o rtss-cli (MIT), o que abre caminho para assinar o executável e reduz alertas falsos de antivírus. (#91, #97, #98, #99)
- As telas salvas, a saída de áudio escolhida e o backup da mesa continuam valendo: os identificadores são os mesmos de antes.

### Atenção: versão alpha
- Esta versão ainda não foi validada em muitos setups de monitores e TV. Se algo falhar ao ligar a TV, desligar as outras telas ou restaurar a mesa, volte para a 1.5.0 e conte o que aconteceu pelo botão de feedback (anexe o `consolemode.log`).
- Quem usa a 1.5.0 estável não recebe esta versão automaticamente.

## [1.5.1]
### Novidades
- Ajustes → "Receber versões de teste (alpha e beta)": entre nos testes da 1.6 pelo próprio app. A 1.6 controla telas e áudio com código próprio, sem as ferramentas da NirSoft. Desligado por padrão; ao ligar, o app avisa que versões de teste podem ter bugs.

## [1.5.0]
### Novidades
- Interface Console: tela inicial e Ajustes em tela cheia, pensados para o controle (direcional/analógico, A/B) com controles Xbox e PlayStation. O modo Automático escolhe essa interface sempre que há um controle conectado. (#41, #42, #47)
- Menu da sessão sobre o jogo: segure Select + Y (Create + △ no PlayStation) para volume, resolução, saída de áudio, limite de FPS, HDR, "Voltar ao PC" e "Sair do Console Mode", em blocos na horizontal. Um aviso no canto lembra a combinação quando o jogo começa. (#46, #64)
- Atalhos do sofá: com o app na bandeja, segure o botão Home para entrar no modo console; durante a sessão, Start + Select volta para o PC. Funcionam também com DualSense e DualShock 4 via HID, sem Steam Input. (#61)
- Automação: links `consolemode://start`, `stop`, `show` e `menu`, `ConsoleMode.exe --stop` e uma API de controle local pelo named pipe `\.\pipe\ConsoleMode.Control`. Por @nextestudios. (#23, #24)
- Ajustes → "Testar controle" mostra ao vivo o que o Windows lê de cada controle e copia um diagnóstico; "Entrar ao conectar um controle"; pasta do Playnite manual para instalações portáteis; "Outros apps do desenvolvedor" (WakeOn, Next Boost). (#63)
- Botão de feedback que abre uma issue no GitHub já preenchida, notas da versão no idioma da interface e interface em espanhol. O instalador pergunta o idioma.

### Correções
- Atualização: o download não esgota mais o tempo em conexões lentas, e a interface Console consegue instalar a atualização, não só encontrar. (#49, #58)
- A navegação pelo controle nos Ajustes da interface Console não pula mais linhas nem desenha o anel de foco no lugar errado, e os botões do aviso de atualização têm anel de foco visível (preto). (#60, #73)
- Controles: um controle com falha não silencia mais os outros, e controles que o Windows lista com atraso (PlayStation, Bluetooth) passam a ser detectados.
- Os itens do menu da bandeja voltaram a funcionar, o app espera a tela de jogo ligar antes de desligar as outras e a janela normal do Steam não é mais confundida com o Big Picture.

### Atualizando da 1.4.0
- O app mostra o aviso: clique em **Atualizar agora**. As configurações são mantidas.
- Se a atualização a partir da 1.4.0 falhar com "HttpClient.Timeout of 15 seconds elapsing" (conexão lenta), baixe o instalador abaixo uma vez. Esse erro é do atualizador da 1.4.0 e está corrigido a partir da 1.5.0.

## [1.5.0-beta.11]
### Novidades
- Menu da sessão (Select + Y) com layout horizontal: blocos lado a lado e "Voltar ao PC" / "Sair do Console Mode" embaixo. No volume, A entra no ajuste (◀ ▶ muda, □/X deixa mudo, A ou B sai). (#64)
- Aviso no canto da tela do jogo alguns segundos depois de entrar: mostra qual combinação abre o menu (Select + Y, ou Create + △ no PlayStation). Não tira o foco do jogo. (#64)
- Ajustes → "Outros apps do desenvolvedor": WakeOn (automação com Wake-on-LAN) e Next Boost (otimização do Windows). (#63)

### Correções
- Menu da sessão: com controle de PlayStation, o menu abria mas o controle não respondia dentro dele. Agora o menu lê o controle direto por HID, mesmo com o jogo em primeiro plano. (#64)

### Versão beta
- Esta beta é a versão principal para download. Quem usa a 1.4.0 ou uma beta anterior recebe o aviso dentro do aplicativo: basta clicar em **Atualizar agora**. As configurações são mantidas. Quem está na beta.8 e vê o erro de tempo esgotado ao atualizar: baixe o instalador abaixo uma vez.

## [1.5.0-beta.10]
### Correções
- Select + Y (menu sobre o jogo) e Start + Back (voltar pra mesa) agora funcionam com controles de PlayStation (DualSense e DualShock 4, USB ou Bluetooth) sem Steam Input: o app lê o controle direto por HID. No controle da Sony: Create/Share + Triângulo abre o menu; Options + Share volta pra mesa; o botão PS vale como Home. (#61)
- Esses atalhos também paravam de funcionar quando "Abrir com o botão Home do controle" estava desligado. (#61)
- Ajustes na interface Console: navegando com o controle, "Como desligar as outras telas" era pulada e o anel de foco aparecia no lugar errado. (#60)

### Versão beta
- Esta beta é a versão principal para download. Quem usa a 1.4.0 ou uma beta anterior recebe o aviso dentro do aplicativo: basta clicar em **Atualizar agora**. As configurações são mantidas. Quem está na beta.8 e vê o erro de tempo esgotado ao atualizar: baixe o instalador abaixo uma vez.

## [1.5.0-beta.9]
### Correções
- Atualização: o download não falha mais com "HttpClient.Timeout of 15 seconds elapsing" em conexões lentas. Agora só é cancelado se ficar 30 s sem receber dados. Quem está na beta.8 e recebe esse erro: baixe o instalador abaixo uma vez; as próximas atualizações pelo app funcionam. (#58)
- Tela inicial Desktop: "Controle detectado" virou um aviso temporário (toast) que aparece por alguns segundos quando um controle conecta, em vez de ficar fixo no cabeçalho. (#58)

### Versão beta
- Esta beta é a versão principal para download. Quem usa a 1.4.0 ou uma beta anterior recebe o aviso dentro do aplicativo: basta clicar em **Atualizar agora**. As configurações são mantidas.

## [1.5.0-beta.8]
### Correções
- Tela inicial Desktop: o aviso "aperte A para o modo console" fica limitado em largura, com reticências, e mostra o texto completo ao passar o mouse; traduções longas não esticam mais o cabeçalho. (#56)

### Versão beta
- Esta beta é a versão principal para download. Quem usa a 1.4.0 ou uma beta anterior recebe o aviso dentro do aplicativo: basta clicar em **Atualizar agora**. As configurações são mantidas.

## [1.5.0-beta.7]
### Novidades
- Ajustes → "Testar controle": mostra ao vivo o que o Windows entrega de cada controle (origem, botões por índice, D-pad, analógicos) e copia um diagnóstico para a issue. O log de abertura lista os controles. Para quem o controle aparece mas não responde (DualSense com Steam aberto, por exemplo).
- API de controle local: com o app aberto, o named pipe `\\.\pipe\ConsoleMode.Control` recebe uma linha JSON (`status`, `start`, `stop`, `show`) e responde com o estado (ativo, restaurando, modo, versão). Só o usuário logado e o LocalSystem conectam. Por @nextestudios. (#24)
- `ConsoleMode.exe --stop` restaura a mesa pela linha de comando, como o `consolemode://stop`. Por @nextestudios. (#23)

### Correções
- Controle: um dispositivo que falha na leitura não silencia mais os outros; controles que o Windows expõe como Gamepad sem XInput responder passam a ser lidos (antes eram pulados); DualSense/DualShock via HID caem no mesmo caminho quando o XInput está vazio.
- Interface Console: o aviso de atualização agora tem um jeito de instalar, não só de encontrar. Antes o botão "Atualizar agora" só existia na tela Desktop; na Console, "Procurar agora" achava a versão nova e não dava pra fazer nada com ela. (#49)

### Versão beta
- Esta beta é a versão principal para download. Quem usa a 1.4.0 ou uma beta anterior recebe o aviso dentro do aplicativo: basta clicar em **Atualizar agora**. As configurações são mantidas.

## [1.5.0-beta.6]
### Novidades
- Interface Desktop navegável pelo controle: D-pad/analógico movem, A ativa (listas, interruptores, cards), B fecha a lista ou volta de Ajustes, Start abre/fecha Ajustes, Y vai para a interface Console; no tutorial, A avança e B pula. Baseado no PR #17 de @nextestudios.

### Correções
- O controle deixava de responder de vez se um controle desconectasse no meio de uma leitura; agora a leitura só pula a amostra. Também vindo do PR #17. (#47)
- Navegação por controle: o primeiro toque só mostra o anel de foco; em listas roladas o foco cai na ordem de tabulação quando a busca espacial não acha nada; diagonais no D-pad contam como vertical; a leitura só vale com a janela em primeiro plano (verificação por janela, não por evento).
- Menu da sessão: com o jogo aberto, segure Select + Y para abrir um menu sobre o jogo com volume, resolução, saída de áudio, limite de FPS, HDR, "Voltar ao PC" e "Sair do Console Mode". Também por `consolemode://menu`.

### Versão beta
- Pré-release: quem usa uma 1.5.0-beta recebe o aviso dentro do aplicativo. Quem está na 1.4.0 não recebe; para testar, baixe os arquivos abaixo.

## [1.5.0-beta.5]
### Novidades
- Interface Console: os ajustes de lista (abrir em, áudio, resolução, FPS, como desligar, idioma, interface) abrem um seletor com todas as opções, com foco na atual; A escolhe, B volta. Antes era preciso ir apertando A para ciclar.
- Na interface Desktop, com um controle detectado, aparece "aperte A para o modo console"; A ou Start trocam a interface.
- Interface em espanhol. Sem escolha salva, o app segue o idioma do Windows (português, espanhol ou inglês). Adicionar um idioma agora é só incluir um `Strings.<código>.json`.

### Correções
- O modo Automático não via controles que o Windows lista com atraso (PlayStation, Bluetooth) e abria em Desktop. Agora o app detecta de novo quando um controle aparece e também 1,5 s e 4 s após abrir.
- Na interface Console, o controle às vezes não respondia até trocar de tela: o listener não ligava na primeira vez.

### Versão beta
- Pré-release: quem usa uma 1.5.0-beta recebe o aviso dentro do aplicativo. Quem está na 1.4.0 não recebe; para testar, baixe os arquivos abaixo.

## [1.5.0-beta.4]
### Novidades
- Opção "Entrar ao conectar um controle" (desligada por padrão): com o app na bandeja, ligar ou conectar um controle entra no modo console. Ignora os primeiros 15 s após abrir e 30 s após restaurar.

### Versão beta
- Pré-release: quem usa uma 1.5.0-beta recebe o aviso dentro do aplicativo. Quem está na 1.4.0 não recebe; para testar, baixe os arquivos abaixo.

## [1.5.0-beta.3]
### Novidades
- Ajustes → "Pasta do Playnite": escolha o Playnite.FullscreenApp.exe manualmente para instalações portáteis, que a detecção automática não encontra.

### Correções
- O idioma escolhido no instalador agora vale para o aplicativo. Sem escolha salva, o app segue o idioma do Windows (português → pt-BR; qualquer outro → inglês) em vez de abrir sempre em português.

### Versão beta
- Pré-release: quem usa a 1.5.0-beta.1 ou beta.2 recebe o aviso dentro do aplicativo. Quem está na 1.4.0 não recebe; para testar, baixe os arquivos abaixo.

## [1.5.0-beta.2]
### Novidades
- Segurar o botão Xbox do controle por 1 segundo, com o app na bandeja, entra no modo console sem tocar no PC. Durante a sessão, segurar Start + Select por 1 segundo volta ao PC. Só controles Xbox (XInput); pode ser desligado em Ajustes.
- Opção "Toque curto no botão Xbox": o app desliga o atalho do controle para a Game Bar e avisa se a Steam também estiver usando o botão.
- Interface Console: cartões grandes em tela cheia, navegados pelo controle (D-pad ou analógico, A, B, Start, Y) ou pelo teclado, com ajustes rápidos que trocam de valor ao apertar A. Em Ajustes → Interface escolha Automático (Console quando há controle), Desktop ou Console; um botão na tela troca na hora.
- Links `consolemode://start`, `consolemode://stop` e `consolemode://show` para automação (Stream Deck, launchers, scripts).

### Versão beta
- Pré-release: quem usa a 1.5.0-beta.1 recebe o aviso dentro do aplicativo. Quem está na 1.4.0 não recebe; para testar, baixe os arquivos abaixo.

## [1.5.0-beta.1]
### Novidades
- Botão de feedback na tela inicial: abre uma issue no GitHub já preenchida com a versão do aplicativo e do Windows, para enviar sugestões e relatar bugs.
- O aviso de nova versão mostra as novidades no idioma da interface.
- O instalador pergunta o idioma, já sugerindo o do Windows. As atualizações feitas pelo aplicativo mantêm a escolha anterior.

### Correções
- Os itens do menu do ícone na bandeja voltaram a funcionar.
- O aplicativo espera a tela do jogo ligar antes de desligar as demais.
- A janela comum da Steam não é mais confundida com o Big Picture.
- As informações do resumo na tela inicial quebram linha em vez de ficarem cortadas.
- O atalho de um clique agora se chama "Console Mode 1 Click".

### Versão beta
- Esta beta é a versão principal para download. Quem usa a 1.4.0 recebe o aviso dentro do aplicativo: basta clicar em **Atualizar agora**. As configurações são mantidas.
- Depois de atualizar, o aplicativo também avisa sobre as próximas betas, além da 1.5.0 final.

## [1.4.0]
### Novidades
- Interface em inglês (Estados Unidos), além do português do Brasil. O idioma é escolhido em Ajustes e muda na hora, sem reiniciar o aplicativo.

### Correções
- Os nomes das resoluções ("Não alterar", "(cache)", "(estimado)") e os botões Ativado/Desativado agora acompanham o idioma escolhido.
- Resoluções guardadas em cache não perdem mais a indicação "(cache)" quando aparecem junto das resoluções estimadas.
- O aviso de nova versão mostra o primeiro item das novidades, e não mais o título "Novidades".

### Como atualizar
- Quem usa a 1.3.0 recebe o aviso dentro do aplicativo: basta clicar em **Atualizar agora**. A versão instalada se atualiza sozinha e a portátil troca o próprio arquivo e reabre.
- As configurações são mantidas. O idioma continua em português até você escolher **English** em Ajustes.

## [1.3.0]
### Novidades
- Interface nativa para escolher em qual tela jogar e o que fazer com as demais.
- Organização das telas conforme a posição real delas na Área de Trabalho.
- Navegação com controle, tutorial inicial e confirmação para ajudar a evitar uma tela sem imagem.
- Ajustes para resolução, taxa de atualização, áudio, HDR, VRR e limite de quadros por segundo.
- Instalador por usuário, sem exigir permissões de administrador, além da versão portátil.
- Aviso de novas versões pelo GitHub, com atualização para as versões instaladas e portáteis.
