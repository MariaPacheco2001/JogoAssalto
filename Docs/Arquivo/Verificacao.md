# Verificação do Marco 1

Data: 9 de outubro de 2026. Editor: Unity 6000.6.5f1 (3ff58d469c8a). Plataforma: Windows x86_64. Cena: `Assets/_GolpeCalculado/Scenes/M01_MovimentoConducao.unity`.

## Método

Abrir a cena no editor, entrar em Play Mode e executar o percurso de validação. Gerar a build Windows e executar o mesmo conjunto no executável. Os controladores recebem comandos de teste equivalentes às ações de jogo. Os testes verificam os resultados da simulação, não apenas a compilação.

O teste de estacionamento coloca a carrinha no lugar para verificar alinhamento, travão, objetivo e saída; não mede a facilidade de estacionar com teclado. As capturas são renderizações das câmaras em execução, sem a interface sobreposta. A utilização completa dos botões do menu e o percurso conduzido por uma pessoa ainda carecem de teste manual.

## Verificações automáticas

1. Menu inicial suspende a simulação.
2. Deslocação a pé corresponde à velocidade configurada.
3. Agachar reduz a cápsula.
4. Levantar restaura a cápsula.
5. Salto ganha altura e regressa ao chão.
6. Entrada junto à porta com linha de visão.
7. Controlo e colisão da personagem desativados enquanto conduz.
8. Aceleração com contacto das rodas.
9. Saída recusada em movimento.
10. Pausa congela posição e áudio; retoma continua a simulação.
11. Direção altera a trajetória.
12. Travão de serviço imobiliza.
13. Marcha-atrás.
14. Estacionamento exige posição, alinhamento, baixa velocidade e travão de mão.
15. Saída pelo lado oposto quando o lado do condutor está obstruído.
16. Regresso ao controlo a pé e atualização do objetivo.
17. Reentrada depois de estacionar.
18. Duas saídas bloqueadas impedem a saída.
19. Saída recupera quando os obstáculos são removidos.

Os resultados finais encontram-se em `Testes/editor-validation.txt` e `Testes/windows-validation.txt`.

## Correções durante a validação

- Atualização da altura da cápsula apenas quando muda o estado de agachar, preservando a deteção de chão para o salto.
- Medição do movimento por frames após estabilização do arranque; o arranque em batch não fornecia uma medição temporal consistente.
- Validação da altura do chão ao sair, para não colocar o jogador em cima de um obstáculo que bloqueia a porta.
- Verificação do volume do jogador e do trajeto de saída para evitar atravessar obstáculos.

## Limitações conhecidas

- O Unity registou uma exceção interna no índice de pesquisa (`UnityEditor.Search.SearchDatabase`) durante o arranque. Não é uma exceção dos scripts do jogo e não surgiu no executável Windows. Registou também avisos internos de alocação ao encerrar o editor.
- A geometria da carrinha, incluindo volante e janelas abertas, o cenário e o som do motor são provisórios. Não há mãos, braços ou animações finais.
- A condução tem aceleração, travões, direção, suspensão e marcha-atrás; o comportamento exige apreciação humana antes do equilíbrio final.
- Não há missão de recuperação da mala, armazém interior jogável, segurança, checkpoints, Continuar ou opções persistentes nesta fase.
- O menu desta entrega só serve o percurso do Marco 1. Os menus completos são requisitos dos marcos seguintes.
- Não foi realizado um teste com uma pessoa sem conhecimento do jogo. A duração da missão ainda não pode ser medida.
- A versão do segundo computador deve ser confirmada pela equipa. O projeto fixa a versão exata, mas esta entrega não verifica um computador que não está ligado à sessão.

## Reprodução

No terminal, pode executar-se a build com `-golpeValidate "C:\caminho\resultados.txt" -logFile "C:\caminho\player.log"`. O teste termina o jogo no fim e cria o relatório e três imagens ao lado do relatório. Usar uma pasta com permissão de escrita.

No editor, o ponto de entrada de linha de comandos é `-executeMethod EditorValidation.Run`, com o mesmo argumento `-golpeValidate`. Para jogar normalmente, não fornecer estes argumentos.
