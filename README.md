# Golpe Calculado

Jogo 3D na primeira pessoa para um jogador, com versão Windows e build experimental macOS, desenvolvido por Rita e Maria. O objetivo final é recuperar uma mala num armazém e levá-la ao esconderijo, descobrindo as relações entre circuitos, câmaras, ruído e comportamento do vigilante.

## Versão e estado

- Unity **6000.6.5f1**, revisão **3ff58d469c8a**, nos dois computadores.
- Projeto: `GolpeCalculado/`. Abrir esta subpasta no Unity Hub, e não a pasta principal do repositório.
- Pipeline Built-In, materiais Standard, sistema de input clássico do Unity para este protótipo.
- Cena: `Assets/_GolpeCalculado/Scenes/M02_AssaltoArmazem.unity`.
- Missão experimental: armazém acessível, ferramentas, chaves, documentos, arquivo, mala, vigilante, câmaras, circuitos, checkpoints e extração. Guia: [Docs/EntregaAssalto.md](Docs/EntregaAssalto.md).
- Duração-alvo: 12 minutos efetivos (mínimo académico 10). Ainda por medir com jogadores novos; distribuição prevista 2/7/3 minutos entre aproximação, infiltração e extração.
- A cena M01_MovimentoConducao continua disponível para testes isolados de movimento e condução.
- A arte e o som de motor são provisórios. A apresentação final, animações, opções completas e validação humana continuam pendentes.

## Abrir pela primeira vez

1. Instalar Unity Hub. Em Installs, instalar exatamente Unity 6000.6.5f1. A versão está também em `GolpeCalculado/ProjectSettings/ProjectVersion.txt`.
2. Instalar o suporte Windows da mesma versão. A build deste marco usa Windows x86_64 e Mono.
3. Obter o repositório e, em Unity Hub > Projects > Add, escolher `GolpeCalculado`.
4. Aguardar a importação. A pasta Library é recriada localmente.
5. No painel Project, abrir a cena indicada acima com duplo clique.
6. Premir Play e depois «Novo jogo» e «Começar o assalto» no menu. Clicar na janela Game se as teclas não responderem.
7. Usar Esc para libertar o rato e abrir a pausa. Para terminar no editor, premir novamente Play.

Não aceitar uma atualização automática de versão no segundo computador. Confirmar 6000.6.5f1 em Help > About Unity antes de editar.

## Controlos

| Contexto | Teclas |
| --- | --- |
| A pé | WASD andar; rato olhar; Shift correr; Ctrl manter agachado; Espaço saltar; E interagir; Q pousar mala; G lançar garrafa; J diário |
| Carrinha | W acelerar; S travar e, depois de parar, fazer marcha-atrás; A/D direção |
| Travões | Shift travão de serviço; Espaço alterna o travão de mão |
| Sair | E, apenas com a carrinha parada e pelo menos uma saída livre |
| Menus | Rato e botão esquerdo; Esc pausa/retoma |

## Teste manual de movimento (cena M01)

1. Andar, correr, agachar e saltar no pátio. Confirmar colisões com o bloco de betão e a carrinha.
2. Aproximar-se da porta esquerda, olhar para a carrinha e premir E. O aviso de interação aparece quando a entrada é possível.
3. Premir Espaço para soltar o travão de mão; acelerar com W, virar com A/D e travar com Shift.
4. Testar S: em andamento trava primeiro; depois faz marcha-atrás.
5. Durante a condução, premir Esc. A carrinha e o som devem parar e continuar ao retomar.
6. Seguir a rua até ao lugar amarelo, alinhar a carrinha, parar e aplicar o travão de mão. O objetivo muda para sair.
7. Premir E. O jogador deve aparecer no chão fora do veículo. Reentrar e repetir.
8. Junto de um obstáculo, verificar a saída alternativa. Com os dois lados obstruídos, a saída deve ser recusada.
9. Na pausa, testar cancelar e confirmar «Recomeçar percurso».

Esta lista complementa os testes automáticos de execução. O teste com uma pessoa que desconhece o jogo ainda precisa de ser realizado.

## Organização para duas pessoas

- Pessoa responsável pela integração: cena principal, jogador, carrinha, interface e builds.
- Segunda pessoa: prefabs de objetos e ambiente numa cena própria de trabalho; posteriormente pistas, sons e testes de utilização.
- Atribuir os papéis à Rita e à Maria antes de começar a editar.
- Trabalhar em branches curtas (`feature/movimento`, `feature/ambiente`). Combinar quem está a editar cada cena; evitar alterações simultâneas ao mesmo `.unity`.
- Partilhar prefabs e os respetivos ficheiros `.meta`. Manter serialização em texto. Não apagar nem regenerar `.meta` de recursos existentes.
- Versionar `Assets`, `Packages` e `ProjectSettings`. Não versionar Library, Temp, Logs ou builds.
- Fazer pull antes de trabalhar e um commit pequeno após testar. A responsável pela integração testa a cena antes de unir branches.

## Criar uma build

No Unity, usar File > Build Profiles > Windows, com M02_AssaltoArmazem em primeiro na lista de cenas. A ferramenta HeistBuilder.Build gera Builds/AssaltoWindows na raiz do repositório. Copiar a pasta inteira para o segundo computador; o executável depende de GolpeCalculado_Data, UnityPlayer.dll e restantes ficheiros.

Não executar «Construir cena Marco 1» para refazer trabalho manual. A ferramenta recusa substituir uma cena já existente.

## Planeamento

Consultar `Docs/EntregaAssalto.md`, `Docs/Planeamento.md` (71 issues), `Docs/PrimeiraAvaliacao.html`, `Docs/InvisibleSystems.md`, `Docs/Assets.md` e `Docs/Verificacao.md`.

## Documento de âmbito em Word

[Descarregar o Scope Document](Docs/Golpe-Calculado-Scope-Document.docx). Versão 1.2, 9 de outubro de 2026: Rita e Maria, sem números de estudante, com duração-alvo de 12 minutos e calendário de 2026.

## Descarregar o jogo — Windows e Mac

[Abrir a release com os dois ZIPs](https://github.com/mjpacheco2001/golpe-calculado/releases).

- **Windows:** Golpe-Calculado-Windows.zip. Extrair tudo e abrir GolpeCalculado.exe.
- **macOS:** Golpe-Calculado-macOS-Universal.zip. Extrair e abrir GolpeCalculado.app. Universal Intel/Apple Silicon, macOS 12+. **Experimental: ainda não testado num Mac real.**
- Cada ZIP inclui um LEIA-ME com instalação, controlos, gravações e resolução de problemas. Não é preciso instalar Unity para jogar.
- Repositório privado: quem usar os links precisa de acesso. Também podes enviar o ZIP diretamente.
- São versões do protótipo; a apresentação final e os testes humanos continuam pendentes.

Detalhes de compilação e critérios macOS em [Docs/MacOS.md](Docs/MacOS.md). A [issue #71](https://github.com/mjpacheco2001/golpe-calculado/issues/71) permanece aberta para os testes num Mac.

## Validação atual

84 verificações no Unity, 84 na build Windows e 4 após reabrir. Detalhes, correções e limites em [Docs/Testes/AuditoriaIssues.md](Docs/Testes/AuditoriaIssues.md). Os testes humanos e a duração continuam pendentes.
