# Echoes — Chapter One

Jogo de terror psicológico em primeira pessoa, feito em **Unity 6000.6.7** (URP Forward+, UI Toolkit, FMOD, Localization).
Projeto de TCC sobre **sanidade em jogos digitais e a representação do TEPT** (memorial em `Assets/Docs`).

O jogador investiga o passado de uma pensão em Harrow Creek e enfrenta o que a mente de Ethan faz com o trauma:
paranoia, alucinação e flashbacks controlados por um sistema de sanidade.

> Status atual: **Fase 3 (a demo)**, branch `fase-3-roteiro`, PR #9 aberto contra `main`.

---

## Onde estamos

### Base técnica (Fases 1–2, concluídas)
- Controle em primeira pessoa, interação, HUD e lanterna.
- **Sistema de sanidade** com limiares (2 Ansiedade, 3 Angústia, 4 Colapso) e `HorrorEventManager`.
- **Checkpoints e save** (`CheckpointManager`), com Continuar / Novo jogo.
- Áudio com **FMOD** (`EventReference`), legendas e textos via **Localization** (tabelas `menu`, `interaction_prompts`, `subtitles`).
- Fluxo de atos em `GameFlow` (`GameAct`: Opening, Prologue, Investigation, Memories, Choir, Ending) e `DemoSettings.IsDemo`, que esconde o conteúdo exclusivo do capítulo completo.

### Fase 3 — roteiro da demo (em andamento)
- **Abertura de 1995 na Pensão Halley**: Clara guia o jogador, fotografa as pistas e fica na porta do quarto.
  - Clara é uma máquina de estados (`Idle, Walking, GoingToDoor, AtDoor, Scripted`): anda quando o Ethan anda e mantém distância fixa até chegar na porta.
  - Modelo próprio (boné com sombra no rosto, casaco marrom, coturno, rabo de cavalo), rig e dedos no Blender, animações Idle/Walking/fotografar e câmera na mão.
  - Acender a luz é uma ação do jogador; o interruptor ligado/desligado alterna os modelos.
- **Abertura do estúdio** (corvo, estilo sketch, só máquina de escrever), splash da Unity + FMOD com a legenda de atribuição.
- **Menu e opções redesenhados**: cenário 3D do quarto, hover com eco, painel de opções escuro e legível, calibração de brilho com o carimbo da delegacia.
- **Tela de carregamento** com o carimbo girando e fade (Continuar, Novo jogo, Voltar ao menu).
- Ícone do jogo (luzes de polícia) e título "Echoes: Chapter One".

### Build
- Windows, **somente DX12**; cookies de luz desligados nos assets URP.
- Build medida (cache frio): **23 min 52 s**, 751 MB, 0 erros. O gargalo é a compilação de variantes de shader (URP Lit ~2,5 mil) e o empacotamento da cena `MenuCenario`.
- Menu `Echoes > Build > Windows (Release, em Downloads)` gera em `Downloads\Echoes_Build` e grava `build_relatorio.txt`.

---

## Onde queremos chegar

**Demo jogável** (vertical slice): abertura de 1995 → apartamento de Ethan em 2003 (investigação) → primeira hora perdida, com sanidade, flashbacks e clima de terror funcionando de ponta a ponta, num build estável.

**Capítulo completo**: Investigação, Memórias, Coro (mecânica do coral) e Final, com todos os eventos de sanidade, vozes e créditos.

---

## O que falta para a demo

**Conteúdo e assets**
- [ ] Gerar os assets da Etapa 2 (apartamento 2003: casaco, cepo de facas, corrente, bloco de repórter) e da Etapa 3 (Pensão Halley 1995), conforme `Assets/Docs/Prompts_Demo_Etapas_2_3.md`, incluindo a faca de cozinha.
- [ ] Gravar as vozes da abertura.
- [ ] Refinar o Ato 1 (ritmo, pistas, posicionamento das fotos).

**Personagem**
- [ ] Aplicar a sombra do boné na textura nova do Meshy (`Models/Clara/base.png`); o `clara.mat` ainda mostra o rosto.

**Interface e legal**
- [ ] Tela de créditos com a legenda do FMOD ("Made using FMOD Studio by Firelight Technologies Pty Ltd.").

**Testes e build**
- [ ] Abrir e jogar o `.exe` gerado (ainda não foi iniciado).
- [ ] Testar Continuar com save real e Voltar ao menu na cena `Game`.
- [ ] Segunda build para medir o tempo com o cache de shaders aquecido.
- [ ] Commitar as mudanças locais de build (`BuildTools.cs`, DX12 only, cookies off).

---

## Estrutura

| Pasta | Conteúdo |
|---|---|
| `Echoes/Assets/Scripts` | Gameplay, sanidade, abertura, UI |
| `Echoes/Assets/Editor` | Ferramentas de setup (menus `Echoes/...`) |
| `Echoes/Assets/Scenes` | PreLoad, Menu, MenuCenario, Abertura1995, Historia_1996/2003, Game |
| `Echoes/Assets/UI` | UXML/USS do menu, opções e HUD |
| `Meshy` | Material de geração de modelos |

Texturas pesadas (png/jpg/exr) usam **Git LFS**.
