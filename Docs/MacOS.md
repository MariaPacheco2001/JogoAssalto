# Suporte macOS

Implementação parcial; teste num Mac real pendente.

## Entrega de 9 de outubro de 2026

- Aplicação .app compilada com Unity **6000.6.5f1 (3ff58d469c8a)**, Built-In, Mono e Metal. Compilação concluída sem erros.
- Módulo **Mac Build Support (Mono)** instalado no computador de compilação Windows.
- Build **Universal**, com **x86_64 e arm64** confirmados nos cabeçalhos Mach-O do executável, UnityPlayer e três bibliotecas Mono.
- Mínimo declarado no Info.plist: **macOS 12.0**. Isto é a configuração da build; desempenho e compatibilidade prática ainda por medir.
- ZIP com a aplicação completa, permissões Unix de execução e LEIA-ME em português. Verificação CRC, SHA-256 e comparação de todos os ficheiros com a build original.
- Existem comandos de assinatura nos binários produzidos pelo Unity. Não foi usada uma identidade Developer ID nem foi feita notarização. A assinatura não foi validada com codesign num Mac.
- Nenhum Mac disponível nesta sessão: **não foi testada a abertura nem a jogabilidade em macOS**.

## Descarregar e abrir

Enviar o ZIP diretamente a quem vai testar.

Extrair Golpe-Calculado-macOS-Universal.zip no Mac, abrir a pasta e mover GolpeCalculado.app para Aplicações. Abrir a aplicação e escolher Novo jogo. O LEIA-ME explica os controlos e o procedimento específico da aplicação em Privacidade e Segurança, caso o sistema não reconheça o programador. Não desativar globalmente as proteções.

## Reproduzir a build

1. Instalar Unity 6000.6.5f1 e o módulo Mac Build Support (Mono).
2. Abrir o projeto GolpeCalculado; não atualizar a versão do editor.
3. Fechar o editor antes da execução em lote. Executar Unity com os argumentos seguintes, substituindo os caminhos:

    -batchmode -projectPath "CAMINHO/GolpeCalculado" -buildTarget OSXUniversal -executeMethod ReleaseBuilder.BuildMac -logFile "CAMINHO/mac-build.log"

A ferramenta escolhe explicitamente a arquitetura x64ARM64 através de EditorUserBuildSettings.SetPlatformSettings. O nome OSXUniversal no argumento de arranque, por si só, não garante as duas arquiteturas. O script recupera a configuração anterior depois da compilação.

Resultado: Builds/AssaltoMacOS/GolpeCalculado.app, na raiz do repositório. Windows usa HeistBuilder.Build com -buildTarget Win64.

Depois de gerar ambas as builds, executar na raiz do repositório:

    python Tools/PackageRelease.py --repo . --output "CAMINHO/Pacotes"

O empacotamento recusa uma aplicação Mac que não contenha as duas arquiteturas. Requer Python 3, sem bibliotecas adicionais. Em macOS, para confirmar a assinatura produzida, usar codesign --verify --deep --strict --verbose=2 GolpeCalculado.app e registar o resultado antes de uma distribuição definitiva.

## Critérios ainda pendentes num Mac real

- [ ] Abrir a aplicação extraída numa pasta limpa e validar as instruções.
- [ ] Confirmar materiais, luzes, áudio, rato, teclado, foco, janela e ecrã inteiro.
- [ ] Concluir a missão: condução, entradas, objetos, mala, segurança e extração.
- [ ] Testar pausa durante condução e perseguição, derrota, vitória e opções.
- [ ] Testar checkpoints e Continuar após fechar e reabrir.
- [ ] Executar a suíte automática da missão e de persistência.
- [ ] Medir desempenho e duração; registar modelo, chip, RAM, macOS e resolução.
- [ ] Validar pelo menos um Apple Silicon e um Intel antes de afirmar que ambos foram testados.

Gravações usam Application.persistentDataPath; não são incluídas no ZIP. Partidas de Windows e Mac não são sincronizadas.

## Referências

- [Unity: builds macOS](https://docs.unity3d.com/6000.6/Documentation/Manual/macos-building.html)
- [Unity: assinatura macOS](https://docs.unity3d.com/6000.6/Documentation/Manual/macoscodesigning.html)
- [Apple: abrir aplicações em segurança](https://support.apple.com/pt-pt/102445)

Dependências de avaliação: #2, #35, #40, #62, #64, #66 e #70. A issue #71 mantém-se aberta até à validação prática.
