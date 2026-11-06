# Planeamento de Golpe Calculado

O projeto tem 71 issues no GitHub, distribuídas pelos cinco marcos. Cada issue identifica objetivo, critérios de aceitação, validação, dependências e prioridade.

## Âmbito atual

Mantêm-se o armazém, a mala, uma carrinha, um vigilante e duas entradas. A 9 de outubro de 2026, a duração pretendida foi ajustada para **cerca de 12 minutos de jogo efetivo** numa primeira conclusão bem-sucedida. Essa meta requer exploração, recolha e utilização de objetos, decisões de segurança e extração. Menus, mortes e esperas artificiais não entram na contagem. O tempo precisa de ser medido com jogadores novos; não está comprovado por uma estimativa do percurso.

## Regra de commits

- Cada commit de implementação referencia as issues a que corresponde, por exemplo: Implementa transporte da mala (Refs #17, #18).
- Uma alteração que satisfaz apenas parte dos critérios usa Refs e mantém a issue aberta.
- Closes é reservado à satisfação integral dos critérios e à validação registada.
- Cada issue tem pelo menos um commit dedicado com uma alteração real de código, testes ou documentação. Os commits anteriores que abrangem várias issues são preservados. Novas alterações devem identificar a issue principal e o tipo de trabalho realizado.
- O commit inicial 311286e é anterior ao planeamento. As issues do trabalho já realizado contêm a referência retrospetiva e a evidência disponível.
- O autor e o email são os configurados no Git. Alterações de identidade exigem indicação explícita da equipa; não se atribui código a outra pessoa por suposição.

As sete issues inicialmente fechadas correspondem ao documento de âmbito e a funcionalidades verificadas do Marco 1. As questões de validação humana, segundo computador e duração permanecem abertas.

## Lista de tarefas


### Marco 1 — Movimento e condução

- [GC-001 · #1](https://github.com/mjpacheco2001/golpe-calculado/issues/1) — Preparar documento de âmbito para a primeira avaliação
- [GC-002 · #2](https://github.com/mjpacheco2001/golpe-calculado/issues/2) — Fixar Unity e validar abertura nos dois computadores
- [GC-003 · #3](https://github.com/mjpacheco2001/golpe-calculado/issues/3) — Avaliar assets candidatos e confirmar pipeline e input
- [GC-004 · #4](https://github.com/mjpacheco2001/golpe-calculado/issues/4) — Implementar andar correr agachar saltar e olhar
- [GC-005 · #5](https://github.com/mjpacheco2001/golpe-calculado/issues/5) — Validar colisões escadas e altura ao levantar
- [GC-006 · #6](https://github.com/mjpacheco2001/golpe-calculado/issues/6) — Criar prefab provisório da carrinha com cabine e escala
- [GC-007 · #7](https://github.com/mjpacheco2001/golpe-calculado/issues/7) — Implementar aceleração direção travagem e marcha atrás
- [GC-008 · #8](https://github.com/mjpacheco2001/golpe-calculado/issues/8) — Implementar entrada na carrinha e troca de controlos
- [GC-009 · #9](https://github.com/mjpacheco2001/golpe-calculado/issues/9) — Implementar saída segura e portas obstruídas
- [GC-010 · #10](https://github.com/mjpacheco2001/golpe-calculado/issues/10) — Validar estacionamento e objetivo do percurso inicial
- [GC-011 · #11](https://github.com/mjpacheco2001/golpe-calculado/issues/11) — Testar menu inicial pausa controlos e reinício do Marco 1
- [GC-012 · #12](https://github.com/mjpacheco2001/golpe-calculado/issues/12) — Validar Marco 1 com jogador novo e entregar build reproduzível

### Marco 2 — Missão completa sem segurança

- [GC-013 · #13](https://github.com/mjpacheco2001/golpe-calculado/issues/13) — Organizar estados de aproximação infiltração e extração
- [GC-014 · #14](https://github.com/mjpacheco2001/golpe-calculado/issues/14) — Construir quarteirão com escolha útil de estacionamento
- [GC-015 · #15](https://github.com/mjpacheco2001/golpe-calculado/issues/15) — Construir interior do armazém com duas entradas acessíveis
- [GC-016 · #16](https://github.com/mjpacheco2001/golpe-calculado/issues/16) — Apresentar briefing e objetivos atuais da missão
- [GC-017 · #17](https://github.com/mjpacheco2001/golpe-calculado/issues/17) — Implementar apanhar transportar pousar e recuperar a mala
- [GC-018 · #18](https://github.com/mjpacheco2001/golpe-calculado/issues/18) — Aplicar limitações de movimento e ação ao transportar a mala
- [GC-019 · #19](https://github.com/mjpacheco2001/golpe-calculado/issues/19) — Carregar a mala na carrinha com estado de carga estável
- [GC-020 · #20](https://github.com/mjpacheco2001/golpe-calculado/issues/20) — Criar esconderijo e condição correta de vitória
- [GC-021 · #21](https://github.com/mjpacheco2001/golpe-calculado/issues/21) — Criar fluxo de derrota e recuperação da missão
- [GC-022 · #22](https://github.com/mjpacheco2001/golpe-calculado/issues/22) — Implementar menu inicial completo e navegação
- [GC-023 · #23](https://github.com/mjpacheco2001/golpe-calculado/issues/23) — Implementar pausa completa e confirmação de ações destrutivas
- [GC-024 · #24](https://github.com/mjpacheco2001/golpe-calculado/issues/24) — Testar missão completa sem segurança do início à entrega

### Marco 3 — Interações e progresso

- [GC-025 · #25](https://github.com/mjpacheco2001/golpe-calculado/issues/25) — Unificar interações e feedback de objetos utilizáveis
- [GC-026 · #26](https://github.com/mjpacheco2001/golpe-calculado/issues/26) — Implementar portas utilizáveis com colisões coerentes
- [GC-027 · #27](https://github.com/mjpacheco2001/golpe-calculado/issues/27) — Implementar chaves e acessos fechados com pistas
- [GC-028 · #28](https://github.com/mjpacheco2001/golpe-calculado/issues/28) — Implementar caixas móveis com limites físicos
- [GC-029 · #29](https://github.com/mjpacheco2001/golpe-calculado/issues/29) — Implementar recolha e lançamento de objetos de distração
- [GC-030 · #30](https://github.com/mjpacheco2001/golpe-calculado/issues/30) — Definir dados de checkpoint com identificadores estáveis
- [GC-031 · #31](https://github.com/mjpacheco2001/golpe-calculado/issues/31) — Implementar CP1 após entrada no armazém
- [GC-032 · #32](https://github.com/mjpacheco2001/golpe-calculado/issues/32) — Implementar CP2 numa zona interior relevante
- [GC-033 · #33](https://github.com/mjpacheco2001/golpe-calculado/issues/33) — Implementar CP3 após recuperar a mala
- [GC-034 · #34](https://github.com/mjpacheco2001/golpe-calculado/issues/34) — Restaurar mundo e segurança sem duplicar objetos
- [GC-035 · #35](https://github.com/mjpacheco2001/golpe-calculado/issues/35) — Guardar progresso em disco e recuperar após reabertura
- [GC-036 · #36](https://github.com/mjpacheco2001/golpe-calculado/issues/36) — Ligar Continuar apenas a progresso válido
- [GC-037 · #37](https://github.com/mjpacheco2001/golpe-calculado/issues/37) — Permitir selecionar checkpoints já desbloqueados
- [GC-038 · #38](https://github.com/mjpacheco2001/golpe-calculado/issues/38) — Confirmar Novo jogo e reinícios com perda de progresso
- [GC-039 · #39](https://github.com/mjpacheco2001/golpe-calculado/issues/39) — Concluir ecrãs de vitória derrota e ações de regresso
- [GC-040 · #40](https://github.com/mjpacheco2001/golpe-calculado/issues/40) — Validar persistência e menus na build Windows

### Marco 4 — Invisible Systems e vigilante

- [GC-041 · #41](https://github.com/mjpacheco2001/golpe-calculado/issues/41) — Definir eventos e prioridades dos sistemas de segurança
- [GC-042 · #42](https://github.com/mjpacheco2001/golpe-calculado/issues/42) — Criar laboratório pequeno dos Invisible Systems
- [GC-043 · #43](https://github.com/mjpacheco2001/golpe-calculado/issues/43) — Implementar câmara com campo de visão e feedback
- [GC-044 · #44](https://github.com/mjpacheco2001/golpe-calculado/issues/44) — Comunicar última posição observada ao vigilante
- [GC-045 · #45](https://github.com/mjpacheco2001/golpe-calculado/issues/45) — Ligar quadro elétrico identificado a luzes e câmara
- [GC-046 · #46](https://github.com/mjpacheco2001/golpe-calculado/issues/46) — Fazer o vigilante investigar falhas elétricas
- [GC-047 · #47](https://github.com/mjpacheco2001/golpe-calculado/issues/47) — Propagar ruído de portas e impactos de objetos
- [GC-048 · #48](https://github.com/mjpacheco2001/golpe-calculado/issues/48) — Implementar patrulha e navegação do vigilante
- [GC-049 · #49](https://github.com/mjpacheco2001/golpe-calculado/issues/49) — Investigar ruídos e regressar à patrulha
- [GC-050 · #50](https://github.com/mjpacheco2001/golpe-calculado/issues/50) — Implementar perceção direta e perseguição com fuga possível
- [GC-051 · #51](https://github.com/mjpacheco2001/golpe-calculado/issues/51) — Procurar na última zona relevante e abandonar a procura
- [GC-052 · #52](https://github.com/mjpacheco2001/golpe-calculado/issues/52) — Ligar captura real à derrota sem penalizar mera deteção
- [GC-053 · #53](https://github.com/mjpacheco2001/golpe-calculado/issues/53) — Implementar alarme com causa descobrível e resposta visível
- [GC-054 · #54](https://github.com/mjpacheco2001/golpe-calculado/issues/54) — Registar pistas descobertas sem revelar regras antecipadamente
- [GC-055 · #55](https://github.com/mjpacheco2001/golpe-calculado/issues/55) — Alterar a segurança depois de recuperar a mala
- [GC-056 · #56](https://github.com/mjpacheco2001/golpe-calculado/issues/56) — Provar duas estratégias e descoberta de uma regra por jogador novo

### Marco 5 — Apresentação equilíbrio e entrega

- [GC-057 · #57](https://github.com/mjpacheco2001/golpe-calculado/issues/57) — Melhorar arquitetura materiais e iluminação do armazém
- [GC-058 · #58](https://github.com/mjpacheco2001/golpe-calculado/issues/58) — Substituir carrinha provisória e afinar condução na missão
- [GC-059 · #59](https://github.com/mjpacheco2001/golpe-calculado/issues/59) — Adicionar mãos braços animações e sombra da personagem
- [GC-060 · #60](https://github.com/mjpacheco2001/golpe-calculado/issues/60) — Criar paisagem sonora e respostas distintas às interações
- [GC-061 · #61](https://github.com/mjpacheco2001/golpe-calculado/issues/61) — Implementar volumes geral música efeitos e vozes existentes
- [GC-062 · #62](https://github.com/mjpacheco2001/golpe-calculado/issues/62) — Implementar opções de vídeo fiáveis
- [GC-063 · #63](https://github.com/mjpacheco2001/golpe-calculado/issues/63) — Guardar sensibilidade inversão legendas e modo de agachar
- [GC-064 · #64](https://github.com/mjpacheco2001/golpe-calculado/issues/64) — Avaliar redefinição de teclas e documentar todos os controlos
- [GC-065 · #65](https://github.com/mjpacheco2001/golpe-calculado/issues/65) — Concluir créditos e registo de licenças dos recursos usados
- [GC-066 · #66](https://github.com/mjpacheco2001/golpe-calculado/issues/66) — Medir desempenho e estabilidade em dois computadores Windows
- [GC-067 · #67](https://github.com/mjpacheco2001/golpe-calculado/issues/67) — Realizar testes com pessoas novas e corrigir confusões
- [GC-068 · #68](https://github.com/mjpacheco2001/golpe-calculado/issues/68) — Equilibrar e medir a missão para cerca de 12 minutos efetivos
- [GC-069 · #69](https://github.com/mjpacheco2001/golpe-calculado/issues/69) — Comparar Assignment 2 e grelha com requisitos e evidências
- [GC-070 · #70](https://github.com/mjpacheco2001/golpe-calculado/issues/70) — Preparar demonstração final build Windows e pacote de entrega

O estado atual de cada tarefa é o apresentado no GitHub. Esta lista é o índice do plano e não substitui o registo de validação.

## Validação por tarefa

As 70 issues têm commits associados: o documento Word na #1 e os [69 guiões individuais](Validacao/README.md) nas restantes. Estes commits de documentação não declaram as funcionalidades concluídas; o estado e as evidências continuam nas issues.

## Plataforma adicional

- [GC-071 · #71](https://github.com/mjpacheco2001/golpe-calculado/issues/71) — Disponibilizar e validar o jogo para macOS. A versão Windows mantém-se; a build macOS Universal e o ZIP foram gerados; revisão prática de compatibilidade e testes num Mac continuam pendentes. Plano em [MacOS.md](MacOS.md).

## Revisão das tarefas e validação

Consultar [AuditoriaIssues.md](Testes/AuditoriaIssues.md): seis issues validadas (#18, #19, #27, #35, #36, #38) e duas tarefas antigas substituídas (#11, #12). As restantes mantêm os critérios pendentes. A meta de 12 minutos exige medição humana e macOS tem uma build experimental e continua por validar num Mac real.

## Revisão seguinte: fecho com evidência

[RevisaoFechoIssues.md](Testes/RevisaoFechoIssues.md) regista 52 verificações adicionais no Unity, 12 tarefas concluídas nesta revisão e os critérios pendentes das 44 restantes. A revisão anterior mantém-se como histórico. ZIPs publicados são uma entrega do protótipo; a demonstração final (#70) e os testes num Mac (#71) continuam pendentes.
