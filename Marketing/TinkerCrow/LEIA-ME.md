# Tinker Crow — materiais de divulgação

**Materiais anteriores, recriados. Para a versão fiel ao jogo solicitada posteriormente, use os arquivos em `Native/`, documentados em `Native/LEIA-ME.md`.**

- `tinker-crow-abertura-4k.mp4`: 3840 × 2160, 30 fps, aproximadamente 5,97 segundos, H.264 e áudio AAC.
- `tinker-crow-perfil.png`: 1254 × 1254, PNG, nova ilustração baseada nos desenhos originais.
- `tinker-crow-video-preview.jpg`: quadro de conferência do vídeo.
- `render_intro.py`: fonte reproduzível da reconstrução do vídeo.

O vídeo foi reconstruído com os sprites, fonte e tempos do projeto. Não é uma gravação da execução no Unity. O papel, os traços e a composição são aproximações; a granulação dinâmica do jogo não foi reproduzida. Os desenhos de origem têm 1024 × 1024 e foram reamostrados para a composição 4K. O som usa a amostra `Menu/typewriter 1.mp3` referenciada pelo evento Typewritter do projeto FMOD local, com uma batida por letra e normalização para evitar clipping.

A imagem de perfil foi gerada com a ferramenta integrada ImageGen. Referências: `corvo_pousado_3.png` e `red_key.png`. Prompt utilizado:

> Create a high quality square 2048x2048 social profile avatar for Tinker Crow, using image 1 as the exact character/style reference and image 2 as the key reference. Black crow perched on a thin hand drawn wire, side profile facing right, holding the metal ring of the antique brass key with its worn red wooden fob firmly in its beak. Preserve original hand drawn pencil, ink, crosshatching sketchbook style, realistic crow anatomy and original silhouette; not cute cartoon, not 3D or photograph. Warm ivory textured sketchbook paper background, subtle pencil vignette. Compose crow and key large, clear and completely inside a centered circle safe area with generous margins so circular profile cropping loses neither beak, key, feet nor tail. Readable dark silhouette at small sizes, refined detailed feathers. The key dangles naturally from the beak, smaller than crow head/body; red fob is only color accent. No letters, no text, no logos, no border. Deliver polished profile artwork preserving the reference identity.

A resolução efetivamente retornada pela ferramenta foi 1254 × 1254. Nenhum arquivo do jogo foi substituído.
