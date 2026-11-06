# Missão do armazém

Versão experimental de 9 de outubro de 2026. Editor Unity 6000.6.5f1. Cena: Assets/_GolpeCalculado/Scenes/M02_AssaltoArmazem.unity.

## O que mudou

A missão acrescenta um armazém com interior, duas entradas, ferramentas, chaves, documentos, um código de acesso, alimentação auxiliar, mala transportável, vigilante, câmaras, circuitos, alarme e regresso ao esconderijo.

O teste do Marco 1 continua na cena M01_MovimentoConducao. A build da missão abre M02_AssaltoArmazem por predefinição.

## Percurso

1. Começar no esconderijo e aproximar a carrinha do armazém.
2. Estacionar e observar as entradas. Há um aviso de manutenção junto à fachada.
3. Procurar a ferramenta para o portão de cargas ou a chave para a porta de serviço.
4. Explorar o cais, o vestiário, a manutenção e o escritório. Recolher chaves e ler documentos.
5. Alimentar o terminal do arquivo e interpretar o código a partir dos registos.
6. Recuperar a mala NC-17 e reagir à alteração da segurança.
7. Transportar a mala até à traseira da carrinha, carregar e regressar ao ponto inicial.
8. Imobilizar a carrinha no esconderijo para concluir.

## Controlos

| Ação | Comando |
| --- | --- |
| Andar e olhar | WASD e rato |
| Correr / agachar / saltar | Shift / Ctrl / Espaço |
| Interagir, ler, recolher, abrir, carregar | E, a apontar para o objeto e perto dele |
| Pousar mala | Q |
| Lançar garrafa recolhida | G |
| Diário de pistas | J |
| Pausa | Esc |
| Carrinha | W acelerar; S travar/marcha-atrás; A/D direção |
| Travões | Shift serviço; Espaço alternar travão de mão |
| Entrar e sair | E; sair apenas com a carrinha parada |

## Sistemas e consequências

- Os quadros B e C controlam luzes e câmaras das respetivas zonas. Podem ser desligados e religados. A falha pode provocar investigação.
- As câmaras comunicam a última posição observada. O vigilante também reage a portas ruidosas, impactos e corrida próxima.
- O vigilante patrulha, investiga, persegue e procura depois de perder visão. A captura causa derrota.
- Os documentos alimentam o diário. As regras dos circuitos só são acrescentadas após a respetiva interação ou observação.
- A mala reduz a velocidade e impede lançar garrafas. Pode ser pousada e recuperada.
- A recolha da mala ativa um alarme com alimentação de reserva. O comando de silêncio exige a chave do escritório.

## Progresso

CP1 é guardado após entrada; CP2 numa zona interior; CP3 após recuperar a mala. O estado inclui inventário, documentos, portas, circuitos, mala, carrinha e objetivo. Ao restaurar, o vigilante volta a uma posição e estado definidos.

Continuar e Selecionar checkpoint só estão disponíveis quando existe um ficheiro reconhecido. A gravação usa a pasta de dados da aplicação no Windows, não a pasta da build. Os testes automáticos usam um diretório próprio, separado da partida do utilizador.

## Âmbito e limitações

Esta versão demonstra o ciclo completo de missão e inclui os primeiros sistemas de segurança. Não corresponde à conclusão de todos os critérios das 70 issues.

- A meta atual é cerca de 12 minutos de jogo efetivo numa primeira conclusão bem-sucedida. O mínimo do enunciado é 10 minutos. Distribuição pretendida: aproximação 2 minutos, infiltração 7 minutos e extração 3 minutos, sem esperas obrigatórias. Ainda não existe uma medição com pessoas novas que comprove essa duração.
- Os testes automáticos exercitam os controladores e estados. Alguns testes reposicionam o jogador para verificar situações específicas; não representam uma partida humana cronometrada.
- A arte é provisória: vigilante e carrinha simplificados, animações e mãos finais por fazer, materiais e som por melhorar.
- As opções desta versão incluem volume geral, sensibilidade, inversão vertical e campo de visão. Faltam os restantes canais, opções de vídeo e restantes preferências.
- Falta testar todos os botões, a leitura das pistas, a dificuldade de fuga e as duas estratégias com pessoas que desconhecem o jogo.
- A grelha completa da Assignment 2 ainda terá de ser recebida e confrontada com o projeto.

Os resultados de execução ficam em Docs/Testes/heist-editor.txt, heist-windows.txt e heist-resume.txt. O estado detalhado das tarefas é mantido nas issues do GitHub.

## Git e equipa

Consultar Docs/Planeamento.md para as 70 issues. Os commits referenciam as tarefas implementadas com Refs #n; usar Closes #n apenas com critérios satisfeitos e validação registada.

A pessoa responsável pela integração edita a cena principal. A outra trabalha em prefabs e cenas de trabalho separadas. Os dois computadores usam Unity 6000.6.5f1 e partilham os ficheiros meta; Library, Logs e builds ficam fora do Git.
