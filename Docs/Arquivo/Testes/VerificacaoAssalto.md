# Verificação da missão do armazém — 9 outubro 2026

Unity 6000.6.5f1, Built-In, Windows x86_64 Mono. Cena M02_AssaltoArmazem.

## Resultado

- 36 verificações aprovadas em Play Mode no editor.
- 36 verificações aprovadas na build Windows final.
- 4 verificações aprovadas num novo processo Windows: ficheiro CP3 existente, carregamento, mala/inventário e circuitos/alarme persistidos.
- Build concluída com zero erros. Sem exceções do jogo nos logs das duas execuções Windows.
- Imagens do exterior, interior e arquivo revistas; corrigidos obstáculos que atravessavam o interior e texto visível através de paredes.
- Guia HTML verificado a 1200 e 390 píxeis, sem transbordo horizontal.

Os relatórios individuais estão nesta pasta. O teste usa um diretório de gravação separado da partida normal.

## Alcance da evidência

As verificações automáticas exercitam componentes em execução e usam reposicionamento e chamadas diretas para isolar situações. Confirmam estados e transições, incluindo linha de visão e perda do jogador, sem constituir uma partida humana do princípio ao fim. A existência de percursos navegáveis pelas duas entradas não comprova, por si, que a dificuldade das duas estratégias esteja equilibrada.

Faltam a sessão humana, o teste de todos os botões e opções, pausa durante condução na nova missão, entradas/saídas com estacionamento livre, robustez perante todos os ficheiros de gravação danificados e validação no segundo computador. A arte, animações e som continuam provisórios.

## Duração

O pedido atual é 12 minutos efetivos, com mínimo académico de 10: aproximação 2, infiltração 7 e extração 3. São orçamentos de design, ainda não medições. A missão não impõe temporizadores para atingir esta meta. Cronometrar pessoas novas, excluindo menus, mortes e esperas artificiais, e ajustar pistas e percurso na issue #68.
