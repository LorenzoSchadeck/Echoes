# Investigação do playtest - 8 de novembro de 2026

## Câmera e sensibilidade

Problemas encontrados no código anterior:

- O mouse fornecia pixels por quadro, mas o driver Cinemachine integrava esse valor por `deltaTime` novamente. A rotação dependia do FPS e quadros demorados podiam ampliar o movimento.
- O sinal vertical positivo era entregue diretamente ao Tilt. No Pan/Tilt, esse sinal olha para baixo: a opção invertY=false não correspondia ao comportamento esperado.
- O eixo era escolhido pelo índice na lista de controladores, ignorando o hint X/Y do Cinemachine.
- Câmeras diferentes habilitavam/desabilitavam a mesma instância de InputAction; desabilitar uma podia cortar a entrada da outra.
- Não havia descarte de delta ao recuperar o foco/travar o cursor, nem bloqueio no controlador durante inspeção/pausa/travas globais.
- PlayerInteractor procurava CinemachineInputAxisController, mas as câmeras usam CustomInputAxisController.

Correções: normalização de unidades mouse/analógico, sinal Y coerente com a opção, leitura única por Update, uso dos hints, ação clonada por controlador, descarte do primeiro quadro após desbloqueio/foco, limpeza de velocidade residual sem alterar ângulos, integração com o controlador correto na inspeção. Sensibilidade ampliada de 0,01-3 para 0,01-10, com passo de teclado de 0,01. Padrão permanece 1; preferências existentes são preservadas. A escala física do mouse mudou e deve ser recalibrada no próximo playtest.

Verificação: assemblies de jogo e Editor compilados com Roslyn/referências locais do Unity 6000.6.7f1 sem erros. Avisos preexistentes de APIs obsoletas/serialização continuam. Harness executado pelo Mono do Unity: 16 verificações passaram usando ConvertLookInput do jogo compilado e DefaultInputAxisDriver real do Cinemachine, cobrindo 30/60/144/240 FPS, quadro de 200 ms, analógico, inversão, limites e entrada inválida. Isso não substitui teste jogável de foco, pausa, inspeção e transições roteirizadas. Não foi possível conectar ao Editor pela CLI/Pipeline.

## Antivírus

Atualização: a investigação posterior está em `Investigacao_Antivirus_2026-11-08.md`. O ZIP original foi localizado, seus 245 arquivos conferem integralmente com a pasta extraída e a varredura atual do ZIP não reproduz a detecção. A hipótese mais forte passou a ser falso positivo na verificação de download, ainda sem confirmação do fornecedor; os resultados iniciais abaixo são preservados como histórico.

O histórico local do Microsoft Defender contém uma detecção de **Trojan:Script/Wacatac.C!ml**, ThreatID 2147749377, associada ao download `Echoes_Build (1).zip`. Na consulta, a ameaça estava inativa e DidThreatExecute=false. Isso não certifica que o ZIP é seguro nem descreve o resultado nos computadores dos testers.

Uma varredura customizada do Defender na pasta extraída `Echoes 0.1.0`, com `-DisableRemediation` (apenas diagnóstico, sem aplicar ações), retornou código 0 e nenhuma ameaça encontrada. A proteção em tempo real estava ativa. A flag não desliga a proteção do sistema.

Assinaturas verificadas: Echoes.exe sem assinatura; UnityPlayer.dll e UnityCrashHandler64.exe com assinaturas válidas. O SHA-256 da UnityPlayer.dll coincide com o template win64_player_nondevelopment_mono da instalação Unity 6000.6.7f1.

Hashes da pasta extraída examinada:

- Echoes.exe: `9EBFBA61EE7CB09E297A75C85342CF66CE063AD17155199A424ACBE03440BFA7`
- UnityPlayer.dll: `38DB99E19730FAD74E8CC21758C87FFE7387B7774EBD9A14405C62AE481F2550`

Conclusão: não é apenas um aviso de baixa reputação. Existe detecção concreta no ZIP, mas ainda não foi possível determinar o arquivo interno responsável nem comprovar que o ZIP bloqueado contém exatamente a pasta escaneada. Não restaurar/desbloquear o ZIP ou orientar desativação de antivírus. Não houve upload de arquivos a serviços externos. O próximo passo é gerar um pacote novo, excluir da distribuição os diretórios BackUpThisFolder_ButDontShipItWithYourGame/BurstDebugInformation, verificar hashes e escanear o ZIP final. Se a detecção persistir, submeter o arquivo ao fornecedor para análise, com autorização do responsável.

O comando de build passa a gerar `sha256.txt` para os arquivos de distribuição. Hashes documentam integridade, não ausência de malware. Assinatura digital não é correção para uma detecção de trojan.

Fontes oficiais:

- Reputação SmartScreen: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation
- Análise de arquivo/falso positivo: https://www.microsoft.com/en-us/wdsi/filesubmission/

## Tela rosa seguida de preta

O relatório da build examinada registra somente Direct3D12, 0 erros e 656 avisos. O projeto também está configurado com apenas DX12. Isso restringe a compatibilidade; não comprova a causa do caso relatado.

Rosa/magenta é o shader de erro da Unity, usado para material/shader ausente, incompatível ou que não compilou. A hipótese principal a investigar é shader/API/driver/GPU. Arquivos ausentes por extração incompleta ou quarentena e exceções na inicialização também precisam ser descartados pelo log.

O comando Echoes > Build foi alterado para incluir Direct3D11 primeiro e Direct3D12 depois. Isso só se aplica ao gerar uma nova build por esse comando; o executável existente não mudou. Em uma build nova com ambas as APIs, `-force-d3d11` e `-force-d3d12` permitem comparação controlada após a verificação do pacote.

StudioIntro esperava indefinidamente por layout do UI Toolkit. Foi acrescentado limite de 10 segundos: registra erro e libera a sequência seguinte em vez de aguardar para sempre. Essa é uma proteção para uma espera sem limite identificada no código, não uma reprodução do problema do tester.

StartupDiagnostics registra no Player.log GPU, API, driver, VRAM, versão do Unity e cenas carregadas, sem transmitir esses dados. Falhas gráficas anteriores à inicialização dos scripts ainda dependem dos logs nativos do Player.

Ainda necessários: GPU e Windows do tester, Player.log completo e confirmação de qual tela foi exibida antes da falha. Local padrão: `%USERPROFILE%\AppData\LocalLow\Lorenzo Schadeck\Echoes_ Chapter One\Player.log` (a substituição de caracteres no nome da pasta pode variar; procurar a pasta Echoes dentro de Lorenzo Schadeck).

Fontes oficiais:

- Shader de erro: https://docs.unity.com/en-us/engine/6000.3/manual/materials-and-shaders/shaders/shader-troubleshooting/shader-error
- APIs DirectX: https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/cross-platform-features/graphics-apis/using-dx11gl3features

## Validação antes de redistribuir

1. Gerar uma nova build pelo comando Echoes > Build e empacotar apenas os arquivos necessários.
2. Escanear a pasta e o ZIP final e guardar seus hashes/resultados.
3. Testar Y padrão e invertido, sensibilidade mínima/máxima, pausa, Alt+Tab e inspeção durante a abertura.
4. Repetir o percurso em FPS alto/baixo, incluindo os giros roteirizados da abertura.
5. Testar inicialização DX11 e DX12 no computador afetado e comparar Player.log.

Não foi gerada nem redistribuída uma nova build nesta investigação. O caso do antivírus e a tela rosa/preta permanecem com diagnóstico parcial.
