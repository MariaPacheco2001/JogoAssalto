# Auditoria de validação de 9 de outubro de 2026

Correções de código no commit 1d2a09b. Unity 6000.6.5f1, cena M02_AssaltoArmazem, build Windows x86_64.

## Resultados

- 84 verificações aprovadas em Play Mode no Unity.
- 84 verificações aprovadas na build Windows.
- 4 verificações aprovadas num segundo processo Windows, após reabrir.
- Build gerada com zero erros. Sem exceções do jogo nos logs Windows destas execuções.

Relatórios: audit-editor.txt, audit-windows.txt e audit-resume.txt. A execução automática usa gravações separadas das partidas pessoais e repõe os dados de teste no fim.

## Correções

O carregamento valida versão, checkpoint, listas, IDs, posições, rotações e coerência entre objetos e inventário antes de alterar o mundo. Uma gravação inválida não é oferecida como válida e não impede recuperar um checkpoint anterior.

Novo jogo e reinício repõem um estado inicial capturado da cena. Confirmar ou cancelar usa a mesma lógica no menu e nos testes; Esc cancela uma confirmação e regressa ao ecrã de origem. A seleção de checkpoint pede confirmação quando abandona progresso. O restauro elimina distrações transitórias, repõe velocidades e reinicia a memória de observação das câmaras.

## Issues validadas

- #18: Os testes confirmam multiplicador de velocidade inferior a 1, impossibilidade de lançar com mala, reposição da velocidade ao pousar e mensagem que explica a limitação.
- #19: Os testes recusam carga na frente, aceitam na traseira, ocultam a mala transportada, recusam repetição e conservam a carga ao entrar e sair da carrinha.
- #27: Os testes confirmam aviso de chave em falta, rejeição de chave diferente, recolha única, abertura com a chave correta e restauro sem duplicação.
- #35: Os testes confirmam escrita e reabertura, rejeição de 15 variantes inválidas e gravação ausente, preservação da sessão após falha e recuperação posterior de uma gravação válida.
- #36: Os testes confirmam seleção do último checkpoint válido, retorno a CP2 quando CP3 é inválido, ausência de progresso após Novo jogo e mensagem recuperável ao falhar leitura. Os botões usam estas mesmas funções de disponibilidade e carregamento.
- #38: Os testes exercitam as mesmas ações usadas pelos botões: confirmar e cancelar Novo jogo, reinício de missão, reinício de checkpoint e regresso ao menu. Comparação dos ficheiros confirma preservação ao cancelar; os reinícios repõem o estado correto.

## Limites e tarefas mantidas

Estes são testes automáticos de componentes em execução, com reposicionamento para isolar casos. Não representam uma partida humana cronometrada, nem uma revisão visual de todos os botões. As issues #22, #23, #25, #30, #34, #37 e #40 permanecem abertas para os critérios restantes, apesar das correções e verificações parciais.

Mantêm-se abertas a cena de laboratório (#42), a demonstração das duas estratégias com pessoa nova (#56), a arte e animações finais (#57–#60), as opções completas (#61–#64), testes em dois computadores (#66), testes humanos e duração (#67–#68), a grelha Assignment 2 (#69), entrega final (#70) e macOS (#71).

As issues #11 e #12 deixam de exigir uma nova sessão sobre o menu e o percurso isolado do Marco 1. Foram substituídas pelos testes de menus e persistência da missão (#22, #23, #40) e pelo teste com participantes novos (#67). O Marco 1 e os relatórios históricos são preservados. Estas duas tarefas são encerradas como não planeadas, não como testes humanos realizados.

## Repetir os testes

No editor, executar EditorMissionValidation.RunAndBuild com o argumento -heistValidate seguido do caminho do relatório. A suite usa a cena da missão e o modo de teste do projeto.

Na build, iniciar GolpeCalculado.exe com -heistValidate e um caminho de relatório gravável. Depois de terminar, iniciar outro processo com -heistResume e -heistValidate para confirmar a leitura do CP3. Não executar duas validações em simultâneo sobre o mesmo diretório de gravação de teste.
