# Investigação do bloqueio de download de Echoes

Data: 08/11/2026. Horários abaixo no fuso de São Paulo (UTC-3).

## Conclusão

Os resultados são mais compatíveis com um falso positivo na verificação de download do Microsoft Defender do que com alteração dos binários de runtime da build. Essa é uma avaliação baseada nas evidências locais, não um laudo do fornecedor nem garantia de ausência de malware.

O bloqueio foi real: o Defender identificou o ZIP baixado como `Trojan:Script/Wacatac.C!ml` e o colocou em quarentena. Atualmente, a varredura do ZIP original e de sua pasta extraída não reproduz a detecção. O responsável confirmou que o ZIP original examinado é o mesmo que compartilhou pelo Google Drive e Discord.

## Cronologia verificada

| Quando | Evidência |
| --- | --- |
| 07/11/2026 15:22:22 | InitialDetectionTime da detecção local associada a `Echoes_Build (1).zip`. |
| 07/11/2026 15:22:30 | Eventos Defender 1116/1117 registram a ameaça e a ação de quarentena concluída. |
| Na detecção | Origem Internet; fonte Downloads e anexos; tipo FastPath; assinaturas AV/AS `1.463.176.0`; mecanismo `1.1.26100.4`. |
| 07/11/2026 19:48:46 | Data da última atualização das assinaturas informada pelo Defender na consulta atual. |
| Consulta de 08/11/2026 | Assinaturas `1.463.186.0`, mesmo mecanismo; proteção em tempo real ativa. |
| Varreduras atuais | Nenhuma ameaça encontrada na pasta `Echoes 0.1.0` nem no ZIP original `Echoes_Build.zip`. |

O registro consultado informa IsActive=false e DidThreatExecute=false. Isso descreve esta ocorrência local; não prova o que aconteceu nos computadores dos testers nem constitui uma análise completa da máquina.

Não há nos eventos consultados um caminho interno do ZIP ou hash da cópia `(1)`. Por isso não foi identificado um arquivo específico responsável pelo alerta, nem foi possível comprovar por hash que a cópia baixada em quarentena era idêntica ao original local. A afirmação de que o original local foi o distribuído é do responsável.

## Comparação do pacote com a pasta extraída

- Arquivo: `Downloads/Echoes_Build.zip`.
- Tamanho: 335.920.204 bytes.
- SHA-256: `8126b9630211f425539a1eb521c23168e9c575e246a10f97494155c3bb17b308`.
- Contém 245 arquivos, todos sob `Echoes_Build/`, com 787.397.857 bytes descompactados.
- Comparação SHA-256 de cada entrada com a pasta `Downloads/Echoes 0.1.0`: **245 idênticas; zero diferentes, ausentes ou extras**.
- A leitura completa das entradas do ZIP terminou sem erro de integridade.
- Há dois executáveis: `Echoes.exe` e `UnityCrashHandler64.exe`.
- Não foram encontrados `.bat`, `.cmd`, `.ps1`, `.vbs`, `.js`, `.scr` ou `.lnk`. Foram encontrados três arquivos `.aspx` de configuração do Mono: `DefaultWsdlHelpGenerator.aspx` nas versões 2.0/4.0/4.5. Seus conteúdos correspondem aos arquivos da instalação do Unity. A presença deles não foi tratada como evidência de trojan.

## Comparação com a instalação Unity 6000.6.7f1

Os seguintes arquivos são byte a byte idênticos aos templates oficiais presentes na instalação local, variante `win64_player_nondevelopment_mono`:

- `UnityPlayer.dll`.
- `UnityCrashHandler64.exe`.
- `MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll`.
- `MonoBleedingEdge/EmbedRuntime/MonoPosixHelper.dll`.

Outros 18 arquivos da pasta MonoBleedingEdge correspondem aos arquivos da instalação, incluindo as três páginas ASPX mencionadas.

O launcher `Echoes.exe` foi comparado ao `WindowsPlayer.exe` do template: as seções `.text`, `.rdata`, `.data`, `.pdata` e `.reloc` são idênticas. A seção `.rsrc` difere, o que é compatível com a personalização normal de ícone e informações do produto. Isso não é uma auditoria completa dos recursos nem do código gerenciado do jogo.

Assinaturas Authenticode: `Echoes.exe` não assinado; `UnityPlayer.dll` e `UnityCrashHandler64.exe` com assinatura válida. O exe não assinado pode ser relevante para reputação, mas não explica sozinho uma detecção de trojan e não deve ser confundido com ela.

Uma busca limitada nos scripts próprios do jogo e Editor não encontrou as chamadas pesquisadas de criação de processos externos, download de arquivos ou carregamento dinâmico de assemblies. Essa busca não equivale a uma auditoria de todo o código ou das dependências.

## Varredura realizada

Comando utilizado, sem executar o jogo:

```powershell
& 'C:\Program Files\Windows Defender\MpCmdRun.exe' -Scan -ScanType 3 -File '<pasta ou ZIP>' -DisableRemediation
```

Nos dois alvos, o comando retornou código 0 e `found no threats`. Segundo a ajuda local do MpCmdRun, `-DisableRemediation` faz a varredura customizada sem aplicar ações, ignora exclusões e inclui arquivos compactados; não desliga a proteção em tempo real.

Nenhum arquivo da quarentena foi restaurado, nenhuma proteção foi desativada, nenhum executável do jogo foi iniciado e nenhum arquivo foi enviado por esta investigação a um serviço externo.

## O que as evidências permitem afirmar

1. O download de uma cópia do ZIP foi bloqueado pelo Defender, não apenas por falta de assinatura.
2. O ZIP original disponibilizado atualmente corresponde integralmente à pasta extraída examinada e passa na varredura atual.
3. Não foi encontrada alteração nos principais binários nativos em relação ao Unity instalado.
4. As assinaturas do Defender mudaram entre o bloqueio e as varreduras atuais. Essa mudança pode explicar um falso positivo que deixou de ocorrer, mas não comprova uma correção específica da Microsoft. A análise de download e a varredura local também podem seguir caminhos diferentes.
5. Os registros não permitem atribuir o alerta a IA, a modelos, a áudios, ao Discord ou ao Drive. O canal compartilhado não determina, sozinho, a causa.

## Aprofundamento no projeto e no código compilado

Esta etapa foi feita após o responsável pedir investigação direta do projeto e da build, além das varreduras iniciais. Não foram executados os binários examinados: a inspeção de IL usou Mono.Cecil para ler metadados e instruções.

### Procedência de todos os binários

Foram inventariados os **179 arquivos EXE/DLL** da pasta distribuída. A comparação por SHA-256 encontrou:

| Correspondência exata | Quantidade |
| --- | ---: |
| Arquivos da instalação Unity (incluindo templates do Player) | 96 |
| Arquivos do Mono instalado com Unity | 23 |
| Artefatos locais da compilação Player em Library/Bee | 52 |
| DLLs presentes nos pacotes do projeto | 4 |
| DLLs dos plugins FMOD no projeto | 3 |
| Exe personalizado, conferido por seções PE | 1 |

Portanto, 178 arquivos têm correspondência integral com uma dessas referências. O restante é Echoes.exe, cujas seções de código e dados coincidem com o launcher Unity, exceto `.rsrc`. Correspondência com arquivos do projeto/artefatos locais confirma a procedência local, não que um terceiro tenha certificado esses arquivos como seguros.

As DLLs nativas dos pacotes são AppUINativePlugin e AppUITextMateLib, em x86_64/ARM64, do pacote Unity App UI. Os plugins FMOD são fmodstudio (x86_64/ARM64) e resonanceaudio (x86_64). Não foi encontrada DLL adicional sem correspondência de origem. A inclusão de ARM64 e componentes de pacotes pode ser revisada para reduzir a distribuição, mas não foi identificada como causa do alerta.

### Código efetivamente distribuído

`Echoes_Data/Managed/Assembly-CSharp.dll` corresponde byte a byte ao artefato `Library/Bee/artifacts/1900b0aP.dag/Assembly-CSharp.dll`.

SHA-256: `b880deac98cd1a89efef3c66281b98e8e6dc3a4d1edd8cc46b1657c738d36600`.

A inspeção estática percorreu **2.375 métodos e 7.064 pares método/chamada distintos**. Nesse assembly:

- Zero importações P/Invoke.
- Zero strings correspondentes aos padrões pesquisados de URL HTTP(S), PowerShell, cmd.exe, rundll32, schtasks, EncodedCommand ou persistência CurrentVersion/Run.
- Não foram encontradas chamadas diretas às APIs pesquisadas de rede, criação de processos, conversão Base64, carregamento dinâmico de assemblies, emissão dinâmica de código ou execução por reflexão.
- Foram encontradas oito chamadas de arquivo/caminho, todas em SaveSystem: leitura, gravação, substituição e remoção de `checkpoint.json` em Application.persistentDataPath.
- As referências a reflexão observadas obtêm nomes de tipos para identificação/logs; não executam métodos carregados dinamicamente.

Isso cobre as instruções presentes no assembly próprio do jogo e padrões específicos de comportamento. Não é execução em sandbox de malware nem prova matemática de ausência de comportamento malicioso em todas as dependências nativas e gerenciadas.

### Fontes e processo de build

Os scripts próprios em Assets/Scripts e Assets/Editor foram pesquisados por chamadas de rede, execução de processos, carregamento dinâmico e mecanismos de pós-processamento. O BuildTools chama BuildPipeline.BuildPlayer e grava relatórios (e, após a correção recente, hashes); não cria ZIP, instalador ou wrapper executável.

Os callbacks de build encontrados em Assets estão no FMOD. Os trechos inspecionados selecionam as bibliotecas de áudio, ajustam compatibilidade de plugins e sinalizam início/fim de build. Não foi identificado código próprio de empacotamento que injete outro programa no resultado.

O packages-lock.json contém dependências de origem registry/builtin; não foram encontrados pacotes git/local/embedded nessa listagem. Isso não substitui a verificação do conteúdo dos pacotes.

Varreduras adicionais do Defender em Assets/Scripts e Assets/Plugins retornaram código 0 e nenhuma ameaça detectada. As varreduras foram de diagnóstico, sem desativar a proteção.

### Estrutura do ZIP

Todas as 245 entradas de arquivo usam compressão Deflate padrão (método 8). Nenhuma entrada é criptografada, não há nomes duplicados, caminhos absolutos/traversal ou comentário do arquivo. O ZIP começa com cabeçalho ZIP padrão, sem wrapper executável anteposto.

Não foi encontrada evidência de arquivo inesperado acrescentado ao pacote, substituição dos binários conferidos ou empacotador executável adicional. Os três ASPX identificados pertencem à configuração padrão do Mono e foram comparados com o Unity instalado; não há evidência de que tenham causado o alerta.

### Limite da conclusão

O projeto e a build examinados não revelaram um componente malicioso responsável pelo alerta. O comportamento observado continua mais compatível com falso positivo no caminho de análise do download. O nome Wacatac e a classificação FastPath no evento não expõem quais bytes, regra ou modelo determinaram a decisão. Como a detecção não se reproduz com as assinaturas atuais, não é possível apontar honestamente um arquivo causador ou atribuir a causa à falta de assinatura, ao Mono, aos ASPX, ao Drive ou ao Discord. A razão exata da classificação permanece pendente de análise do fornecedor.

Os resultados detalhados estão em `AnaliseAntivirus/Procedencia_binarios.json` e os scripts/resultados auxiliares em `tmp/virus_investigation/` na raiz do repositório.

## Próximos passos para confirmação externa

- Preservar o ZIP original e este SHA-256, sem modificá-lo para tentar evitar o detector.
- Se houver novo bloqueio, registrar nome da ameaça, data, versão das assinaturas e hash do pacote no computador afetado, quando acessível sem restaurar quarentena.
- Encaminhar o ZIP e esses dados ao Microsoft Security Intelligence como desenvolvedor, informando a suspeita de falso positivo. A submissão envolve enviar a build a terceiros e ainda não foi autorizada nem realizada.
- Para o próximo playtest, gerar uma build nova, empacotar apenas arquivos necessários e verificar tanto a pasta quanto o ZIP final. Remover do pacote de distribuição o diretório `Echoes_BackUpThisFolder_ButDontShipItWithYourGame` é higiene de distribuição, não uma correção comprovada para Wacatac.

Fontes oficiais consultadas:

- Microsoft: tecnologias de detecção e machine learning: https://learn.microsoft.com/en-us/defender-endpoint/adv-tech-of-mdav
- Microsoft: análise de arquivos originados da Internet: https://learn.microsoft.com/microsoft-365/security/defender-endpoint/configure-block-at-first-sight-microsoft-defender-antivirus
- Microsoft: análise de arquivos e falsos positivos: https://www.microsoft.com/en-us/wdsi/filesubmission/

Resultados auxiliares da comparação local estão em `tmp/virus_investigation/`. O relatório descreve os arquivos da build anterior; alterações de gameplay feitas depois não foram incluídas nesse ZIP.
