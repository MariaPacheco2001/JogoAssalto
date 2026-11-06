# Revisão de fecho de issues — 9 de outubro de 2026

Foram revistas as 56 issues abertas contra os critérios e o estado real. Esta revisão conclui 12: #13, #20, #21, #23, #30, #33, #34, #45, #50, #53, #54 e #65. Resultado previsto após sincronização: 27 fechadas e 44 abertas, num total de 71. As restantes conservam o trabalho pendente descrito abaixo.

## Evidência e limites

- Código do jogo: a96bb99 (mesma lógica e recursos dos ZIPs publicados). Esta revisão acrescenta uma ferramenta de teste apenas no editor; não altera o jogo distribuído.
- 52 verificações adicionais aprovadas no Unity 6000.6.5f1, cena M02_AssaltoArmazem. Relatório: [audit-followup.txt](audit-followup.txt).
- Evidência anterior: 84 verificações Windows, 4 após reabrir e 84 no ZIP extraído, em [Release-2026-10-09](Release-2026-10-09). Os 52 casos novos foram executados no editor, não são apresentados como testes Windows.
- Os testes usam reposicionamento e chamadas às mesmas ações da aplicação para isolar situações. Não substituem partidas contínuas, testes com pessoas novas ou medição de duração.
- A última execução registou uma ArgumentOutOfRangeException no indexador UnityEditor.Search.SearchDatabase durante o arranque do editor. A suíte terminou com 52 PASS; não houve exceção da lógica de jogo. Este registo não é descrito como uma execução sem qualquer aviso do editor.
- Créditos e recursos: [InventarioRecursos.md](InventarioRecursos.md), código dos créditos e Docs/Assets.md. A integração de novos assets obriga a atualizar atribuições.

## Tarefas concluídas

- **#13:** Fases e objetivos verificados na sequência normal e fora de ordem; recolha repetida não recria CP3 nem duplica mala; reinício recupera aproximação.
- **#20:** Chegar ao esconderijo sem mala mantém a missão; chegar com mala carregada e veículo parado apresenta vitória uma única vez. O percurso corresponde ao quarteirão de condução já entregue.
- **#21:** Três ciclos de captura e restauro aprovados, com simulação/áudio suspensos na derrota e controlo recuperado no checkpoint. Deteção à distância inicia perseguição sem derrota.
- **#23:** Pausa na carrinha aprovada na build Windows anterior; pausa durante perseguição agora aprovada no Unity, incluindo posição, alvo e tempo. Destinos de pausa revistos no código; confirmações usam as ações já testadas.
- **#30:** Snapshot lido do disco inclui jogador, mala, veículo, chaves, portas, circuitos e progresso; versão 1 e IDs únicos verificados. Vigilante regressa explicitamente à ronda na posição de restauro.
- **#33:** CP3 criado na obtenção da mala; restauro depois de transportar ou pousar recupera a mesma posse e a ronda definida. Posição reaparecida está fora do alcance de captura e da visão imediata.
- **#34:** Alterar portas, inventário, notas e circuitos e restaurar recupera o mundo; eventos de distração e investigação elétrica pendente são eliminados; resta uma só mala. O teste aguarda além do atraso da investigação para confirmar o cancelamento.
- **#45:** Os dois quadros desligam e religam luzes e indicador da câmara em conjunto; o restauro preserva o estado desligado. Etiquetas das zonas e resposta sonora/textual verificadas na cena e implementação.
- **#50:** Alcance, cone e parede verificados; visão exposta inicia perseguição, cobertura permite fuga e conserva última posição; pausa congela perseguição e temporizadores.
- **#53:** Manifesto anuncia sensor e bateria antes da recolha; recolha ativa alarme e resposta do vigilante. Testados os dois circuitos sem cortar alarme de reserva, silêncio e restauro do alarme ativo.
- **#54:** Diário inicial sem soluções; documentos e ações descobertas acrescentam pistas; pausa disponibiliza-as e leitura do checkpoint em disco restaura todas as chaves e textos. A reabertura geral de saves já tem testes Windows separados.
- **#65:** Inventário atual de Assets conferido com os créditos: apenas recursos do próprio projeto e dependências Unity. Rita e Maria identificadas; candidatos da Asset Store não integrados. Licenças dos pacotes mantidas. Rever créditos quando forem integrados recursos nas tarefas de arte ainda abertas.

## Trabalho que permanece aberto

- **#2:** Falta clonar e abrir no segundo computador, registar Play sem erros e acordar a responsável por cada cena. Versão exata e ficheiros do projeto já estão no Git.
- **#3:** Os candidatos foram consultados e o protótipo usa controladores próprios/Built-In. Falta confirmar recursos finais compatíveis com o estilo realista e testar isoladamente qualquer pacote que venha a ser adotado.
- **#5:** Falta executar o percurso de colisões com teto baixo, degraus, inclinações, porta estreita e recuperação de posição inválida.
- **#14:** Quarteirão existe. Falta comparar os dois estacionamentos em jogo, demonstrando efeitos de distância e visibilidade na extração.
- **#15:** Interior e duas rotas até ao arquivo têm caminhos NavMesh completos. Falta percorrer fisicamente entrada e regresso por ambas com jogador/mala, verificando passagens e obstruções; não falta criar o interior de raiz.
- **#16:** Briefing e objetivos contextuais já existem. Falta uma pessoa nova reconhecer o objetivo inicial e as mudanças de fase sem explicação.
- **#17:** Recolha, transporte, pousar e recuperação existem. Faltam casos de obstrução, volume livre para pousar e revisão visual da mala em vários locais.
- **#22:** Todos os destinos existem e a lógica de progresso/Novo jogo foi testada. Falta percorrer a interface completa na build, incluindo voltar de cada destino e Sair.
- **#24:** Ciclo automático e ZIP Windows disponíveis. Faltam duas partidas contínuas, uma por entrada, com duração ativa preliminar registada; reposicionamento dos testes não substitui essas partidas.
- **#25:** Raio, alcance, parede e objeto baixo estão testados. Falta a matriz de alvos concorrentes e feedback de todos os tipos de interação.
- **#26:** Abrir/fechar e estado persistido existem. Falta movimento animado da folha e testar colisões junto do jogador/mala de ambos os lados.
- **#28:** Caixa recebe impulso físico e tem feedback. Faltam testes junto de paredes/passagens e recuperação quando bloqueia um percurso.
- **#29:** Recolha/lançamento e bloqueio ao transportar mala existem. Falta validar trajetória e impactos reais no chão e parede, incluindo evento de ruído.
- **#31:** CP1 é criado à entrada. Falta testar entrada e restauro seguro pelas duas rotas, incluindo portas e inventário de cada estratégia.
- **#32:** CP2 interior existe. Falta testar restauro com combinações diferentes de portas/chaves e confirmar acessos desimpedidos.
- **#37:** Seleção, confirmação e bloqueio de CP inexistente estão testados. Falta destacar visualmente o checkpoint ativo; a lista atual mostra também futuros desativados.
- **#39:** Vitória/derrota e ações foram exercitadas no Unity. Falta percorrer os respetivos botões e opções na build Windows, com e sem progresso disponível.
- **#40:** Há 84 verificações Windows, 4 após reabrir e teste do ZIP extraído. Falta matriz integral dos botões/checkpoints na build; o novo teste de perseguição foi no editor.
- **#41:** Existem chamadas de ruído/câmara e prioridade da perseguição. Falta modelo/documentação de eventos com origem, instante e prioridades, além de repetição determinística das sequências.
- **#42:** Falta a cena separada do laboratório com duas rotas, pistas e reset. A missão principal não substitui automaticamente esse laboratório.
- **#43:** Câmara muda de indicador e deixa de observar sem energia. Falta testar especificamente limites do cone, alcance, oclusão e interrupção das atualizações da câmara.
- **#44:** A câmara transmite uma posição ao vigilante. Falta executar deteção pela câmara, deslocação oculta e expiração do alvo sem seguir a posição atual do jogador.
- **#46:** Falha elétrica agenda investigação e perseguição tem prioridade no código. Falta executar os casos durante ronda e perseguição e confirmar a consequência de religar antes da investigação.
- **#47:** Portas e impactos chamam o sistema de ruído. Falta provar alcance/intensidade e estabilidade perante sons repetidos dentro e fora do alcance.
- **#48:** Ronda e navegação existem. Falta observar várias voltas e bloqueio de passagens/portas fechadas sem atravessamento.
- **#49:** Investigação tem destino e temporizador. Falta observar a deslocação completa, a oportunidade noutra zona e o regresso à ronda em repetições.
- **#51:** Perda de visão usa a última posição conhecida. Falta observar o fim da procura e a prioridade de novos estímulos durante os 18 segundos de procura.
- **#52:** Deteção à distância não derrota; captura verifica visão e proximidade no código. Falta teste real de contacto e contacto separado por parede, incluindo unicidade da derrota.
- **#55:** Mala anuncia/ativa alarme e CP3 restaura esse estado. Falta completar a extração contínua pelas duas rotas com as limitações de transporte.
- **#56:** Faltam as duas estratégias completas e uma pessoa nova que descubra uma regra e a aplique depois sem explicação.
- **#57:** Existe cenário funcional com materiais e iluminação provisórios. Falta a apresentação realista final e a revisão de legibilidade das pistas em zonas claras/escuras.
- **#58:** Existe carrinha provisória funcional. Falta modelo final com cabine, revisão de cortes/colisões e apreciação da condução por pessoas novas.
- **#59:** Faltam mãos/braços, poses e animações finais e sombra humana credível, revistos nas interações e na carrinha.
- **#60:** Existem motor e sinais sonoros provisórios. Falta paisagem sonora coerente, sons distintos de passos/portas/impactos e ajuste espacial.
- **#61:** Volume geral persiste. Falta canal de efeitos e decisão sobre música/vozes efetivamente existentes, com teste de aplicação, pausa e reabertura.
- **#62:** Campo de visão persiste. Faltam resolução, janela, qualidade, avaliação de brilho e reversão de alterações inválidas/não confirmadas.
- **#63:** Sensibilidade e inversão persistem. Falta alternar/agachar, reposição dos valores e decisão de legendas consoante conteúdo de fala.
- **#64:** Mapa de controlos está no jogo e LEIA-ME. Falta decisão documentada de remapeamento, tratamento de conflitos e reposição de valores caso seja adotado.
- **#66:** Falta medir uma missão completa em dois computadores Windows, com hardware, qualidade e tempos de frame registados.
- **#67:** Falta sessão com participantes que não conhecem o jogo, registo de confusões, correções e repetição.
- **#68:** 12 minutos é a meta; não existe medição humana que confirme mínimo de 10 e alvo de 12 minutos efetivos. Falta cronometrar e ajustar.
- **#69:** Falta receber/analisar a grelha completa da Assignment 2 e ligar cada critério à evidência. O Scope Document da primeira entrega já está publicado.
- **#70:** ZIPs Windows/macOS com LEIA-ME já publicados e Windows testado após extrair. Falta demonstração final pista→descoberta→decisão→consequência, partida integral e conferência da grelha final; é uma entrega de protótipo.
- **#71:** Build Universal e ZIP Mac verificados estaticamente e publicados. Falta abertura, partida, persistência e desempenho num Mac real; continua experimental.

## Repetir a verificação

Fechar o editor e invocar Unity com -batchmode -projectPath "CAMINHO/GolpeCalculado" -buildTarget Win64 -executeMethod IssueAudit.Run -issueAudit "CAMINHO/relatorio.txt" -logFile "CAMINHO/editor.log".

IssueAudit.cs fica em Assets/_GolpeCalculado/Editor. Usa o diretório de gravações de teste, não a partida pessoal. Não executar outra validação simultânea. A ferramenta termina com código 0 apenas quando todos os casos passam. A execução limpa os checkpoints de teste no fim.
