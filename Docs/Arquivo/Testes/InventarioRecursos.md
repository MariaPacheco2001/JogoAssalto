# Inventário de recursos — revisão #65

9 de outubro de 2026. Inventário do protótipo publicado e das ferramentas de teste no projeto.

Assets contém apenas _GolpeCalculado. Modelos/cenário/prefabs e materiais procedem de PrototypeBuilder e HeistBuilder; EnginePrototype.wav é escrito por PrototypeBuilder.EngineSound. Não foram integrados os três candidatos da Asset Store. Créditos em HeistGameUI: Rita e Maria, Unity e recursos provisórios do projeto. Documentação de origem em Docs/Assets.md.

As dependências de Packages/manifest.json são módulos Unity e integrações de editor. As licenças fornecidas continuam nos pacotes; não se versiona Library. A revisão de novos recursos e das respetivas atribuições deve acompanhar as issues #57 a #60.

| Tipo | Ficheiros |
| --- | --- |
| .asset | 2 |
| .cs | 21 |
| .mat | 19 |
| .prefab | 2 |
| .shader | 1 |
| .unity | 2 |
| .wav | 1 |

| Ficheiro | SHA-256 |
| --- | --- |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Alarme vermelho.mat | 6358da9417702df0dbf370ba9138b4a149318f972d255ccb122e5e6d1f6865b3 |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Amarelo segurança.mat | 2a9bca3923275fba5a8a4746772c53b7361298b41f9f086a25f8b8d35ca5da04 |
| GolpeCalculado/Assets/_GolpeCalculado/Art/ArmazemNavigation.asset | f7b13c36453dac225de7c58d042625322c80bca68f7566743a7ae888cf3dc69c |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Asfalto.mat | dc1b42236e7ed9ab06ef1a59699f564443af348ecfe30d3fc7511a654c4ba732 |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Aço.mat | 2756e91b4e25c7e9099fdacc30c2b82d43b5ab0700dfe571367589af4adb28b8 |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Betão industrial.mat | 1cf58922dfd58bb72449b86596de49ce8f04bf41c87822c1941338f2079c19d1 |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Betão.mat | 553dbe6767f1b713c9a3197b958d4b95eab79bfb542bb933823a5d037e59ab1e |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Borracha escura.mat | 987e28af011208beaca9b424f0a2d830815002109fec34e42370c5dea74bc65b |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Borracha.mat | b5bd620673b2c39ca8cd9999fe0ea41b1db1f4f52c395833773cef5ef0fc39e8 |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Carrinha Cinza.mat | e97f777032f5663ac9d5f56cca3bb6780f68ac92cbb926f3be43ccff4828b330 |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Ceu.mat | f61543336cadc970e6da7f7c7e70e29e2729b8b043213edb7da0d02c6b91f3d4 |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Madeira caixas.mat | 5b24c43cc9784a376e464ec8ea8947bf72592ab810c1781883cf68a2784f1b48 |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Marfim.mat | 41ca858f4073115f6e2315621030daf65a092e57b5ce4b0e272b800ab2b2333c |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Metal industrial.mat | 4f74b2b3bdf109cc7683d1bb2d39602cfe17781640b441698fa2cc76397105ef |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Papel e luminárias.mat | 19d9d72457f7823d381db73e36aa186158f0f4f6c3a3e4db526ae7a39706ebe9 |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Sinalização.mat | bf49389ccf54272c9664e7d3b823cfe681d0b860ccbe48103dfa845b83af073e |
| GolpeCalculado/Assets/_GolpeCalculado/Art/TextoComProfundidade.mat | c85d6cc67497ebc6133d89e7c8f05a80ccfbb4eb5f9abb19205da73599966baf |
| GolpeCalculado/Assets/_GolpeCalculado/Art/TexturaBetao.asset | 500e9b2bd87d2ac0fd63260d0842103c65263b6c0c57a2dc6aead684a7f90ddf |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Tijolo.mat | bf2cc86cff7384b41277da92918409a08ae032725fbde30dd8d6233afc81a972 |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Vestuário vigilante.mat | ec82023441ea1bbcf50eaf4b5436060000b9e2d1ae889ca71f96cff5428a5172 |
| GolpeCalculado/Assets/_GolpeCalculado/Art/Vidro Escuro.mat | 0aac0bf4ce8233f96b3b643f2d0f5ce47b23d1cf265d0baf2d98cdf2325ec91e |
| GolpeCalculado/Assets/_GolpeCalculado/Art/WorldText.shader | c41d1823f9eb9be5232e552769f3a68ae56787d42548b69b5fcb68e87987c998 |
| GolpeCalculado/Assets/_GolpeCalculado/Audio/EnginePrototype.wav | 2139b12015fa91c7b333f3c3fe38614ad9f58500f5fdd1aee92476fd7a6354e0 |
| GolpeCalculado/Assets/_GolpeCalculado/Editor/EditorMissionValidation.cs | 859a94a770cc48880cfe90c467ed335668caedc5ab29b3c463db74d8f2beb5d7 |
| GolpeCalculado/Assets/_GolpeCalculado/Editor/EditorValidation.cs | 569ef345358506aa5fff4270dabf9ede43335bea36ac1397e98f65129bc47ad5 |
| GolpeCalculado/Assets/_GolpeCalculado/Editor/HeistBuilder.cs | 2c7315dc42ec0166957ad13edc57bd700df51607c4062e7cc4c002373563cf7e |
| GolpeCalculado/Assets/_GolpeCalculado/Editor/IssueAudit.cs | 11eb6490fa0713cdd798bfe94292720e2bcb689bf5bcd02186da91ffcb1f607f |
| GolpeCalculado/Assets/_GolpeCalculado/Editor/PrototypeBuilder.cs | 64feb0fe1d05b997920e29d132bcb01648df9966b4200862228a9de407504636 |
| GolpeCalculado/Assets/_GolpeCalculado/Editor/ReleaseBuilder.cs | 286bda445a6954f0f4456be6ab3ab0be7e74dcc78e4b35eafa573aa1c94c5f27 |
| GolpeCalculado/Assets/_GolpeCalculado/Prefabs/Carrinha.prefab | 7be63edb45b293b85f8e9a784bfbefa850cef008418f04bd5821e78bbe7cad5d |
| GolpeCalculado/Assets/_GolpeCalculado/Prefabs/Jogador.prefab | fc52c07c4fefc5240213dcc85e0a910ea60d3b68e9195dd9946366c4fbbdd53e |
| GolpeCalculado/Assets/_GolpeCalculado/Scenes/M01_MovimentoConducao.unity | 31a9e583b1ff29067e8d31ad7772093792d0b5247a0ed43c9034b00fd57d1c3b |
| GolpeCalculado/Assets/_GolpeCalculado/Scenes/M02_AssaltoArmazem.unity | 4dd6cc1c33c5f07ac2ea69218fc071a97417ad780cfc28699c480d7c6717dcbe |
| GolpeCalculado/Assets/_GolpeCalculado/Scripts/HeistCamera.cs | 7b7e05200e29335e08eff5eef89894114131ea2eb510d61770e4e073f0202f6b |
| GolpeCalculado/Assets/_GolpeCalculado/Scripts/HeistGame.cs | abd8f507ad5948609d16456fe9ad5e10ae3d00f35cfd9bdf09820636bd4c0e03 |
| GolpeCalculado/Assets/_GolpeCalculado/Scripts/HeistGameUI.cs | 0983c4fe0c63438917cdd73b917e5f587d45b95a73681ac9ca8a7a64df76496b |
| GolpeCalculado/Assets/_GolpeCalculado/Scripts/HeistGuard.cs | 7413476b8494019eece71d5233fe3c4e04f0f0aab1226ebe1def461de4428036 |
| GolpeCalculado/Assets/_GolpeCalculado/Scripts/HeistItem.cs | 2525137bb5a249556c2c990a80bed017dfdc335803e5a67190a2ae93925e7a65 |
| GolpeCalculado/Assets/_GolpeCalculado/Scripts/HeistMenuActions.cs | 8178b87ed25dd34f052ebb78c20f1e37195422065e4e261aa76b397d404f4f2d |
| GolpeCalculado/Assets/_GolpeCalculado/Scripts/HeistNavigation.cs | 4471c87dbb3212be7f97fc151036ad57db2701db5f23cee11d786a5a4fef7829 |
| GolpeCalculado/Assets/_GolpeCalculado/Scripts/HeistPersistence.cs | 45562383903ad5709b13bc7e3a4fddd928d4773261610666aa30e401fb87665b |
| GolpeCalculado/Assets/_GolpeCalculado/Scripts/HeistRobustnessValidation.cs | da036ac2681c77e106667bca2159dbf7de0eb97f00fbf884f084140f365ddfce |
| GolpeCalculado/Assets/_GolpeCalculado/Scripts/HeistValidation.cs | 7d8ecf69527f7965ab21a35fcd1c5abbe2318e77bd21778d15333c502ba7bdbf |
| GolpeCalculado/Assets/_GolpeCalculado/Scripts/PlayerMotor.cs | c277dcabc20ddec2782d15f39f046c161a23012139f2d667a1a4a45624290758 |
| GolpeCalculado/Assets/_GolpeCalculado/Scripts/PrototypeSession.cs | b933a87002d8ad027f5d27053b4144d37271f1155ce1f067e2c02c0fc9e9b2d5 |
| GolpeCalculado/Assets/_GolpeCalculado/Scripts/RuntimeValidation.cs | 3db2477fb8feedd33b050948ec773f9362ceecb2ca8aaf34a5b39d420a9035a8 |
| GolpeCalculado/Assets/_GolpeCalculado/Scripts/VanMotor.cs | 35cead49d9ffc49c7f56a701177e28e88de18fcb8b094b237de3a012fa1f7e4a |
| GolpeCalculado/Assets/_GolpeCalculado/Scripts/WorldText.cs | 4e7df4d45a86867c6c621ed2e532515dfe4335a4d5450250fb889ab90a19b0c6 |
