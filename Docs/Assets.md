# Escolhas técnicas e recursos

Verificação em 9 de outubro de 2026. Editor instalado: Unity 6000.6.5f1, revisão 3ff58d469c8a. A anterior referência a 6000.6.1f1 não corresponde à instalação atual.

## Decisão para o Marco 1

Built-In, shader Standard, CharacterController e Rigidbody com quatro WheelColliders. Input Manager clássico, sem dependências externas para mover ou conduzir. A carrinha e o ambiente são geometria de protótipo; o motor usa uma pequena amostra de áudio sintetizada por código. A finalidade é validar escala, transições, colisões e condução. Estes recursos não representam o nível visual final.

Os controlos só pertencem a um contexto de cada vez. Ao entrar, desativam-se o movimento e a cápsula do jogador; a câmara passa para o lugar do condutor. Ao sair, verifica-se chão e espaço livre antes de reativar a personagem. O travão de mão é aplicado automaticamente.

## Candidatos consultados

| Recurso | Evidência pública e decisão |
| --- | --- |
| [Demo City By Versatile Studio](https://assetstore.unity.com/packages/3d/environments/urban/demo-city-by-versatile-studio-mobile-friendly-269772) | Página indica versão 1.0, Unity original 2021.3.9 e compatibilidade Built-In/URP/HDRP nessa versão. Está classificado como low poly. Não adotado para a apresentação realista; não foi importado nem foi confirmado um interior utilizável |
| [Mini First Person Controller](https://assetstore.unity.com/packages/tools/input-management/mini-first-person-controller-174710) | Página indica versão 1.2.0, Unity original 2020.3.12 e os três pipelines. A integração no Unity 6000.6.5f1 não foi testada. Não adotado; o controlador próprio evita integrar comportamentos de personagem duplicados |
| [Realistic Car Controller](https://assetstore.unity.com/packages/tools/physics/realistic-car-controller-16296) | A designação «Free» apresentada no pedido não foi confirmada como pacote específico. A [documentação do fabricante](https://www.bonecrackergames.com/bonecrackergames.com/admin/AssetStoreDemos/RCC/NEW4/HTML/01_OverviewAndFeatures.html) descreve uma solução baseada em WheelCollider. Não adquirido nem importado. Preço, licença da edição concreta, dependências e input precisam de confirmação antes de o integrar |

As páginas dos dois primeiros candidatos indicam a Standard Unity Asset Store EULA. Isso não equivale a autorizar redistribuição dos pacotes originais num repositório público. Nenhum dos três recursos foi incluído neste marco, pelo que não aparece como recurso utilizado nos créditos.

## Direção visual seguinte

Manter um quarteirão industrial modular com medidas reais, um armazém com interior construído e uma carrinha de carga. Selecionar posteriormente um conjunto PBR coerente de betão, chapa, asfalto e madeira e uma carrinha com cabine/interior e licença identificada. Importar primeiro numa cena isolada; verificar materiais, luz, colisões, escala, portas e visibilidade do condutor antes de substituir o protótipo. Reavaliar URP antes da fase de arte se os recursos efetivamente escolhidos o justificarem.

## Créditos deste marco

Equipa: Rita e Maria. Modelos, materiais e som provisórios construídos dentro do projeto. Plataforma e motor: Unity. Não há modelos, controladores ou amostras de som externos importados. As licenças dos pacotes Unity permanecem nos respetivos pacotes.
