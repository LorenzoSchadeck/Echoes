# Fechamento da demo — 09/11/2026

Escopo deste chat: apartamento de Ethan em `ApartamentoDemo`. A abertura e a distribuição entram como dependências do fechamento geral; ainda precisam de revisão própria. Este checklist atualiza o diagnóstico do README, que ainda lista modelos já presentes no projeto.

Percurso esperado: abertura de 1995 → chegada ao apartamento em 2003 → exploração e boletim → investigação no mural → corredor e olho mágico → primeira hora perdida → descoberta das mudanças → cartão final → menu.

## Já implementado ou configurado

- [x] Cena independente `ApartamentoDemo`; `Game` não está na lista de cenas da build.
- [x] Mobiliário montado para o apartamento; revisão visual e de colisões ainda pendente.
- [x] Mural com foco de câmera, bandeja de pistas 3D e pensamentos de Ethan.
- [x] Posicionamento livre, pinos importados e fios tubulares dinâmicos.
- [x] Persistência das pistas e conexões fora da interação; restauração do layout verificada em teste anterior.
- [x] Catálogo com recorte do Halley, boletim, bilhete do vizinho e relatório clínico; duas deduções configuradas.
- [x] Sistemas de clímax, hora perdida e encerramento presentes na cena. Presença não significa percurso validado.
- [x] Rádio restaurado na cópia da demo, com câmeras, legendas, mostrador e papéis de progressão. O rádio havia sido removido durante a montagem da cena. A cena original não foi modificada.
- [x] Rádio validado em Play: instância FMOD em reprodução, desligamento pela interação, avanço para `Investigation` e adição do boletim ao mural. A proteção de tempo mínimo para desligar foi acelerada apenas na sessão de teste; o trigger espacial e o tempo integral continuam pendentes de playtest.

## 1. Progressão e pistas — bloqueia o percurso normal

- [ ] Colocar as pistas definitivas em locais coerentes do apartamento e configurar inspeção/coleta. Na auditoria inicial havia **zero `EvidenceSource`** na cena; os quatro coletores temporários não substituem a exploração.
- [ ] Conferir `NeighborNoteDelivery.note` e a entrega do bilhete, incluindo coleta após carregar checkpoint.
- [ ] Conferir a descoberta do relatório clínico e do recorte; o boletim deve entrar pelo rádio.
- [ ] Revisar os modelos dos documentos e os textos visíveis, incluindo data, lugar e pensamentos em português/inglês.
- [ ] Ajustar referências ao antigo bloco/anotação (`IV_TAKE_NOTE` e `BulletinEvidence`) à decisão de usar a interpretação no mural.
- [ ] Validar as duas deduções pelo percurso real e manter ligações livres sem disparar conclusões narrativas incorretas.
- [ ] Retirar da distribuição os papéis temporários e preservar sua utilidade apenas no Editor.

## 2. Rádio, ritmo e eventos

- [ ] Conferir posição, escala, colisão e alcance do rádio restaurado em relação à mobília atual; revisar os dois papéis recuperados junto dele.
- [ ] Percorrer o trigger inicial andando, ouvir o boletim e conferir entrada/saída da interação.
- [ ] Conferir os áudios FMOD contra o roteiro e sincronizar legendas; os tempos de roteiro foram estimados por texto.
- [ ] Ajustar demora do bilhete, eventos aleatórios, deduções necessárias e fallback do clímax, evitando eventos sobrepostos.
- [ ] Validar clímax com e sem olhar pelo olho mágico.

## 3. Hora perdida e encerramento — referências quebradas confirmadas

- [x] Reparar `LostTimeEvent.changes[3].target` (corrente solta) e `[5].target` (faca), vazios na auditoria. Corrente aberta instanciada do prefab existente; faca recuperada da cena original. Os seis alvos estão preenchidos; posicionamento e troca em jogo ainda precisam de revisão.
- [ ] Conferir as versões presa/solta da corrente, o encaixe na porta e a troca durante o apagão.
- [ ] Conferir posição e escala do cepo e da faca, com desaparecimento perceptível após o apagão.
- [ ] Conferir casaco seco/molhado, gotas, poça, pegadas e posição de despertar.
- [ ] Validar salto do relógio para 01:13, retorno dos controles e encerramento ao examinar o casaco molhado.
- [ ] Validar fallback do encerramento e retorno ao menu sem travas de câmera, cursor ou áudio.
- [ ] Testar Continuar após a hora perdida: reaplicar o mundo e garantir que o encerramento permaneça acessível. `DemoEnding` atualmente depende do evento transitório `OnLostTime`; não foi validado o retorno por save.

## 4. Cena, sensação de controle e desempenho

- [ ] Playtest de câmera ao andar/olhar, pausa, Alt+Tab, inspeção, mural, olho mágico e retomada do controle.
- [ ] Revisar colisões, gavetas, armários, objetos flutuando e caminhos bloqueados.
- [ ] Revisar iluminação e legibilidade de pistas/fios no ambiente escuro.
- [ ] Medir CPU/GPU em Play e na build. A cena contém 1.037 `DeformableObject`; é um ponto para medição, não uma causa comprovada das travadas.
- [ ] Conferir sons de passos/superfícies, ambiente, sustos e prioridades entre voz interna e rádio.

## 5. Save e fluxo completo

- [ ] Novo jogo até o apartamento, sem depender de abrir a cena manualmente.
- [ ] Save real antes/depois da coleta, deduções, entrega do bilhete, clímax e apagão.
- [ ] Continuar em cada estágio, sem duplicar pistas, sons ou eventos e sem impedir a progressão.
- [ ] Voltar ao menu e iniciar novamente sem estado estático residual.

## 6. Dependências do restante da demo — a confirmar fora deste chat

- [ ] Revisar abertura de 1995: vozes finais, sincronização, ritmo, fotos e transição para 2003.
- [ ] Conferir textura/sombra do boné da Clara; o README ainda registra pendência.
- [ ] Conferir créditos, atribuições e licenças, incluindo FMOD.
- [ ] Conferir opções, idiomas e textos finais em todo o percurso.

## 7. Candidato a distribuição

- [ ] Gerar nova build Windows com DX11 padrão e DX12 disponível, pelo fluxo atualizado de build.
- [ ] Jogar o executável do início ao fim; conferir logs, materiais e cenas incluídas.
- [ ] Testar desempenho e controles em outra máquina, incluindo o relato de tela rosa/preta.
- [ ] Empacotar apenas arquivos de distribuição; conferir hashes e varredura da pasta/ZIP conforme investigação anterior.

Ordem de execução: resolver progressão e referências quebradas → testar o percurso no Editor → revisar arte/áudio/ritmo → validar saves → gerar e testar a build. Itens desmarcados incluem trabalho pendente e verificações ainda não realizadas; não são todos defeitos comprovados.
