# Cena de teste dos Invisible Systems

## Objetivo

Antes de decorar o armazém, validar se uma pessoa sem conhecimento prévio descobre uma relação causal e a aplica numa segunda decisão. Cena futura: `M04_LaboratorioSistemas`. A proposta abaixo ainda não está implementada no Marco 1.

## Disposição

Um corredor em U, uma sala com mala, duas entradas e uma divisória que corta linhas de visão. Entrada A: porta silenciosa, vigiada por câmara. Entrada B: porta metálica ruidosa, fora do campo da câmara. Um vigilante patrulha três pontos. Um quadro «Corredor B» controla a câmara e duas luzes. Uma placa junto da mala indica «Base com sensor — alarme independente». O alarme tem alimentação de reserva explicitamente identificada.

## Regras constantes

| Pista anterior à ação | Ação e reação percetível | Consequência |
| --- | --- | --- |
| Etiqueta «Corredor B», cabo até à câmara e luz verde de alimentação | Desligar quadro: estalido, luzes apagam, indicador da câmara apaga | Câmara deixa de observar; vigilante investiga a posição conhecida do quadro depois de 2 segundos. Religar restaura câmara e luzes |
| Câmara gira, tem LED e delimita uma zona com sinal de videovigilância | Exposição por 1 segundo: aviso sonoro e LED vermelho | Envia a última posição realmente observada; ao perder visão o vigilante investiga essa posição, sem seguir o jogador através de paredes |
| Dobradiças degradadas e aviso «Porta ruidosa»; objeto metálico à disposição | Porta abre com rangido ou objeto embate com som | Se estiver a até 12 m, o vigilante investiga a origem; a ronda fica temporariamente livre |
| Placa de base com sensor e cabo até à sirene de reserva | Levantar mala: sirene e luz vermelha | Vigilante dirige-se à sala e procura; o circuito das luzes não desliga este alarme |

Prioridades: visão direta do jogador > última posição observada > alarme > ruído/falha elétrica > patrulha. Eventos iguais, em estados iguais, produzem a mesma resposta. Perder visão inicia procura limitada na última zona relevante; depois o vigilante regressa à ronda. Apenas contacto de captura causa derrota.

## Duas estratégias a validar

1. Observar a ronda, desligar a câmara, aguardar a deslocação do vigilante para o quadro e entrar por A. Sair por B, já tendo preparado o trajeto.
2. Manter as luzes, lançar um objeto para afastar o vigilante e entrar por B. Observar o varrimento da câmara para atravessar A na extração.

O alarme da mala altera ambos os planos. A mala reduz a velocidade e impede lançar objetos; pousá-la possibilita uma nova distração, mas exige voltar para a recuperar.

## Sessão de utilização

Entregar apenas o objetivo «Recupera a mala e regressa ao início». Não explicar as relações entre dispositivos. Registar: primeira pista vista; hipótese verbalizada; ação; reação percebida; decisão posterior que reutiliza a regra; confusões; tempo ativo. Após a sessão, perguntar «O que mudou quando desligaste o quadro?» e «Que plano usarias na próxima tentativa?».

Critério de passagem: a pessoa identifica uma relação sem explicação prévia e usa-a para escolher ou alterar o percurso. Não basta premir o interruptor por acaso. Repetir com outra pessoa após melhorar pistas que tenham falhado. O teste continua pendente; não há resultados humanos registados.

## Demonstração curta

Mostrar etiqueta do circuito; desligar; observar luzes, câmara e reação do vigilante; usar a deslocação para atravessar; recuperar a mala; adaptar a saída ao alarme. Reservar 60–90 segundos para demonstrar a relação causal, sem confundir essa demonstração com a duração total da missão.
