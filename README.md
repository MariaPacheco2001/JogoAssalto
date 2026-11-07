# Golpe Calculado

Jogo 3D na primeira pessoa, para um jogador, feito por Rita e Maria. Recuperas uma mala num armazém vigiado e levas-la ao esconderijo, descobrindo como os circuitos, as câmaras, o ruído e o vigilante estão ligados.

## Abrir o projeto

1. Instalar no Unity Hub exatamente o **Unity 6000.6.5f1** (com suporte Windows). Não aceitar atualizações de versão.
2. Em Projects > Add, escolher a pasta `GolpeCalculado` (não a pasta principal do repositório).
3. Abrir a cena `Assets/_GolpeCalculado/Scenes/M02_AssaltoArmazem.unity` e carregar em Play.
4. No menu: «Novo jogo» e «Começar o assalto». Esc liberta o rato e abre a pausa.

A cena `M01_MovimentoConducao` serve só para testar o movimento e a condução.

## Controlos

| Contexto | Teclas |
| --- | --- |
| A pé | WASD andar; rato olhar; Shift correr; Ctrl agachar; Espaço saltar; E interagir; Q pousar mala; G lançar garrafa; J diário |
| Carrinha | W acelerar; S travar e marcha-atrás; A/D direção; Shift travão; Espaço travão de mão; E sair (parada) |
| Menus | Rato; Esc pausa/retoma |

## Onde está cada coisa

| O quê | Onde |
| --- | --- |
| Cena da missão, com a Hierarchy em pastas | `Scenes/M02_AssaltoArmazem.unity` |
| NavMesh: objeto «Navegação armazém» (NavMesh Surface). Depois de mudar paredes ou estantes, carregar em **Bake**. | `Scenes/M02_AssaltoArmazem/` |
| Prefabs: estante, luminária, câmara, porta, mesa com documento, quadro elétrico, garrafa, caixa, garrafa lançada | `Prefabs/Missao/` |
| Interface: objeto «Interface» (Canvas); os botões estão ligados em On Click | cena M02 |
| Quadros elétricos: cada quadro tem no Inspector as luzes e a câmara que controla (On Switched) | cena M02 |
| Zonas dos checkpoints CP1 e CP2 (caixas Is Trigger) | «Missão Golpe Calculado / Zonas de checkpoint» |
| Sons das respostas | `Audio/Efeitos/` e lista «Sounds» do HeistGame |
| Texturas e materiais (cor, tamanho da textura e brilho no Inspector) | `Art/` e `Art/Texturas/` |
| Valores afináveis (vigilante, câmaras, código do cofre, distâncias, ruído, portas) | Inspector de HeistGuard, HeistCamera, HeistGame e HeistItem |

O código (`Assets/_GolpeCalculado/Scripts`) fica só com os sistemas do jogo: vigilante, câmaras, objetos interativos, jogador, carrinha, gravação e a troca de ecrãs. A etiqueta git `antes-do-editor` guarda o estado anterior à passagem para o editor.

## Builds

File > Build Profiles > Windows, com a M02 em primeiro lugar, ou o menu Golpe Calculado > Build Windows (missão), que grava em `Builds/AssaltoWindows`. Copiar a pasta inteira. A build macOS está descrita em [Docs/MacOS.md](Docs/MacOS.md) e ainda não foi testada num Mac.

## Testes automáticos

Correm com o Unity fechado (substituir CAMINHO):

```
Unity.exe -batchmode -projectPath "CAMINHO/GolpeCalculado" -executeMethod EditorMissionValidation.Run -heistValidate "CAMINHO/heist.txt" -logFile "CAMINHO/heist.log"
Unity.exe -batchmode -projectPath "CAMINHO/GolpeCalculado" -executeMethod IssueAudit.Run -issueAudit "CAMINHO/audit.txt" -logFile "CAMINHO/audit.log"
Unity.exe -batchmode -projectPath "CAMINHO/GolpeCalculado" -executeMethod EditorValidation.Run -golpeValidate "CAMINHO/m01.txt" -logFile "CAMINHO/m01.log"
```

## Trabalho a dois

- Combinar quem edita a cena; só uma pessoa de cada vez no `.unity`. Os prefabs ajudam a dividir o trabalho.
- Não apagar nem regenerar ficheiros `.meta`.
- Branches curtas, pull antes de começar e commits pequenos depois de testar.

## Créditos

- Texturas: [ambientCG](https://ambientcg.com) (CC0).
- Efeitos sonoros: [Pixabay](https://pixabay.com/sound-effects/) (licença Pixabay).
- Modelos, controladores e restante conteúdo feitos no projeto.

## Documentação

- [Docs/Golpe-Calculado-Scope-Document.docx](Docs/Golpe-Calculado-Scope-Document.docx): documento de âmbito.
- [Docs/EntregaAssalto.md](Docs/EntregaAssalto.md): guia da missão.
- [Docs/InvisibleSystems.md](Docs/InvisibleSystems.md) e [Docs/Assets.md](Docs/Assets.md).
- `Docs/Arquivo/`: planeamento antigo, guiões por issue e relatórios de testes, guardados como histórico.
