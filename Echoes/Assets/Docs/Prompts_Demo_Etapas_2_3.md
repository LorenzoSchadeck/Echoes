# Echoes: prompts dos modelos da demo (etapas 2 e 3)

Mesmo formato do arquivo de prompts anterior: uma imagem de frente por asset, com o prompt já completo, e um negative prompt para usar junto. Gere a imagem, passe pelo gerador de 3D e coloque o modelo em `Assets/Models/<nome do asset>/`, como os outros. Me avise quando estiverem no projeto: eu ajusto escala, texturas e colisão, e monto as cenas.

**Limites de peso**

- **Pequeno:** até 2 mil triângulos, textura 512 ou 1K.
- **Médio:** até 8 mil triângulos, textura 1K.
- **Grande:** até 15 mil triângulos, textura 2K.

**O que não precisa gerar** (eu faço no Unity ou já existe no projeto):

- **Já existem:** rádio-relógio (`radio clock`), portas (`Porta.fbx`), chaves (`chave .fbx`), olho mágico, aquecedor (prompt no arquivo anterior).
- **Eu monto:**
  - corredor e quarto da pensão: paredes, piso e teto em ProBuilder, com as texturas do fim deste arquivo
  - lâmpada pendente do corredor, pegadas molhadas e mancha no lençol (decalques)
  - números nas portas e o "6" no chaveiro
- **Sugestão para cortar:** a mão de Ethan acendendo a luz na abertura. Ela pede um braço com rig e animação. Sem ela, a luz acende com o clique do interruptor quando Ethan entra, e a fala de Clara ("Como você achou o interruptor no escuro?") continua plantando o detalhe.

---

## Etapa 2 · Ato 1, apartamento de 2003

### Casaco no gancho (médio)

O casaco fica seco no começo e encharcado na hora perdida; o molhado é material e gotejamento, feitos no Unity.

```
men's long dark charcoal wool overcoat hanging from a single brass wall hook by its collar loop, heavy fabric folds, slightly worn cuffs, 1990s style, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
person, mannequin, body, head, hands, coat rack, hanger stand, multiple coats, wall, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, modern, glossy, logo, text, cropped, perspective distortion
```

### Cepo de facas com um espaço vazio (pequeno)

```
wooden kitchen knife block with five slots, four black-handled kitchen knives inserted and the largest top slot empty, worn varnished wood, 1990s home kitchen, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
all slots filled, loose knives, blood, cutting board, food, counter, hands, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, modern, stainless steel block, glossy, logo, text, cropped, perspective distortion
```

### Corrente de porta (pequeno)

Peça pequena e fina, que os geradores costumam errar. Se sair ruim, eu faço com peças simples no Unity.

```
brass door chain guard lock, a slide track plate with a short hanging chain ending in a small round knob, separate small receiving plate, tarnished brass, 1980s apartment door hardware, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
door, padlock, keys, doorknob, hands, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, modern, chrome, digital lock, logo, text, cropped, perspective distortion
```

### Bloco de repórter com lápis (pequeno)

Fica ao lado do rádio; é onde Ethan anota o boletim.

```
reporter's spiral-bound notepad with a top flip cover folded back, pages covered in faint unreadable pencil scribbles, a yellow wooden pencil lying across it, slightly curled paper corners, 1990s, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
readable words, printed text, laptop, phone, desk, hands, multiple notepads, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, modern, glossy, logo, cropped, perspective distortion
```

---

## Etapa 3 · Abertura de 1995, Pensão Halley

A pensão é um prédio velho: os móveis são dos anos 60 e 70, gastos, vistos numa noite de 1995.

### Cama de solteiro de ferro (grande)

```
1960s single iron bed frame with simple vertical bars at head and foot, chipped white paint, thin striped mattress made with a plain white sheet and one flat pillow, cheap boarding house bed, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
person, body, blood, double bed, canopy, headboard cushion, nightstand, rug, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, modern, glossy, logo, text, cropped, perspective distortion
```

### Lençol sobre o corpo (médio)

Só a forma sob o lençol; a mão e a mancha eu acrescento no Unity. Alternativa: cobrir o `Cadaver1.fbx`, que já está no projeto, com um lençol simulado.

```
a plain white bedsheet draped over the shape of an adult lying on their back, the sheet covers everything from head to feet, soft realistic cloth folds, only the outline of the shape is visible, no body parts visible, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
visible face, visible skin, hands, feet, blood, gore, wounds, bed, mattress, pillow, ghost costume, eyes, standing figure, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, glossy, logo, text, cropped, perspective distortion
```

### Mesa de cabeceira (médio)

```
1970s small wooden nightstand with one drawer and a small round knob, open shelf below, peeling wood veneer, scratched top, cheap boarding house furniture, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
lamp, clock, books, objects on top, bed, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, modern, white lacquer, glossy, logo, text, cropped, perspective distortion
```

### Abajur (pequeno)

```
1970s bedside table lamp, round ceramic base in faded mustard yellow, pleated cream fabric shade yellowed with age, short power cord, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
nightstand, table, light bulb glowing, light rays, multiple lamps, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, modern, LED, metal minimalist, glossy, logo, text, cropped, perspective distortion
```

### Cômoda (médio)

A carteira da vítima fica em cima dela.

```
1960s low wooden dresser with three wide drawers and small round wooden knobs, scratched dark veneer, water rings on top, cheap boarding house furniture, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
mirror, objects on top, open drawers, clothes, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, modern, white lacquer, glossy, logo, text, cropped, perspective distortion
```

### Pia de parede (médio)

```
old wall-mounted white porcelain sink, chipped enamel edges, rust stains near the drain, two cross-head chrome taps, exposed chrome pipes underneath, 1960s boarding house bathroom fixture, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
mirror, cabinet, countertop, wall tiles, soap, towel, water running, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, modern, vessel sink, glossy, logo, text, cropped, perspective distortion
```

### Guarda-roupa (grande)

```
1960s tall narrow wooden wardrobe with a single door, small metal keyhole, worn dark varnish, slightly warped, simple boarding house furniture, door closed, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
open door, clothes, mirror door, double doors, carvings, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, modern, white lacquer, glossy, logo, text, cropped, perspective distortion
```

### Armário de cozinha com a gaveta de talheres aberta (médio)

É a pista "falta a faca maior".

```
small 1970s kitchenette base cabinet with a worn laminate countertop, the top drawer pulled open showing mismatched cutlery inside and one obviously empty space, one small cupboard door below, chipped paint, cheap boarding house kitchenette, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
sink, stove, appliances, dishes on top, knife visible, blood, wall cabinets, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, modern, stainless steel, glossy, logo, text, cropped, perspective distortion
```

### Fogareiro elétrico (pequeno, opcional)

Só atmosfera, em cima do armário.

```
1970s single burner electric hot plate, enamel body in faded cream, black coil burner, one round control knob, short power cord, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
pot, pan, food, flame, glowing red coil, counter, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, modern, induction, glass top, glossy, logo, text, cropped, perspective distortion
```

### Carteira da vítima, aberta (pequeno)

O documento sai em branco; o nome Ruth Pierce entra como textura no Unity.

```
1990s women's brown leather bifold wallet lying open, card slots, a small blank identification card in the clear window, a few folded bills, worn leather edges, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
readable name, photo face, credit card logos, coins scattered, purse, handbag, hands, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, modern, glossy, logo, text, cropped, perspective distortion
```

### Cartão de Ruth Pierce (imagem para textura)

Segue o estilo dos porta-retratos do jogo: foto bem desfocada, escura e com granulação de filme, sem nada legível. Os dados do cartão só aparecem no painel de inspeção, quando o jogador examina a carteira. Formato: 1,586 por 1, por exemplo 2048 x 1291 px.

O prompt evita falar em documento ou identidade, porque essas palavras fazem o gerador acusar possível golpe. A imagem desfocada também não serve como documento.

```
heavily out of focus analog photograph of a small old worn laminated plastic card from the 1990s, the card fills the whole frame, pale blue and beige tones, a tiny portrait of a tired woman with brown hair tied back on the left side of the card, faint pale lines on the right side, everything soft and blurry so nothing can be read, strong defocus blur, visible film grain, dim warm lamp light, muted dark colors, moody vintage photo look, texture for a horror video game prop
```
**Negative prompt**
```
sharp focus, in focus, readable text, letters, words, numbers, signature, logos, seals, emblems, flags, barcodes, holograms, chips, official design, hands, wallet, table, background around the card, tilted angle, glare, bright colors, modern design, clean
```

**Texto do painel de inspeção** (eu escrevo nas tabelas de idioma):

- Nome: Ruth Ann Pierce
- Endereço: Rua Halley, 41, quarto 6, Harrow Creek
- Nascimento: 14/03/1960 (34 anos na noite de 9 de fevereiro de 1995)
- Emitido em 1993, válido até 1997

### Chave com chaveiro de madeira vermelho (pequeno)

Vai ser usada duas vezes: na fechadura do quarto 5 e, no final do capítulo, no quarto trancado.

```
old brass room key attached by a small metal ring to a red painted wooden key fob shaped like an elongated teardrop, paint worn at the edges, blank fob without numbers, 1960s boarding house room key, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
numbers, engraved text, multiple keys, keychain bundle, car key, plastic fob, lock, door, hands, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, modern, glossy, logo, text, cropped, perspective distortion
```

### Janela de guilhotina (médio)

A janela do quarto, por onde entra a luz da viatura.

```
old wooden double-hung sash window with two single-pane glass panels, peeling white paint, small brass latch in the middle, simple wooden frame and sill, window closed, 1950s apartment building, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
curtains, blinds, view outside, landscape, wall, brick, multiple windows, stained glass, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, modern, PVC, aluminum frame, glossy, logo, text, cropped, perspective distortion
```

### Escada de incêndio (grande)

Vista pela janela; é por onde Ethan sai no fim da abertura.

```
black wrought iron fire escape landing section with a slatted floor, waist-high railing and a straight ladder going down from one side, rusty and wet, old New York style apartment fire escape, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
building, brick wall, windows, street, multiple floors, spiral staircase, people, other objects, extra props, scene, multiple views, split image, collage, three-quarter view, top view, modern, painted bright colors, glossy, logo, text, cropped, perspective distortion
```

### Lâmpada (pequeno)

Uma lâmpada incandescente comum, sozinha, para reaproveitar em vários objetos (corredor, abajures, luminárias). O brilho e o pisca-pisca são feitos no Unity; o vidro deve vir apagado.

```
single classic incandescent light bulb, clear glass with visible tungsten filament, standard brass screw base, slightly dusty glass, light off, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
glowing bulb, light rays, lens flare, socket, cord, lamp shade, fixture, LED bulb, spiral CFL bulb, frosted glass, multiple bulbs, hands, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, glossy, logo, text, cropped, perspective distortion
```

### Luminária de teto do quarto (pequeno, opcional)

A luz que Ethan acende no quarto 6.

```
1960s small flush ceiling light fixture, frosted white glass dome shade with a simple metal rim and center screw cap, yellowed glass, cheap boarding house room light, light off, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
glowing, light rays, chandelier, pendant cord, LED panel, modern design, ceiling, multiple fixtures, hands, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, glossy, logo, text, cropped, perspective distortion
```

### Interruptor de parede (pequeno, opcional)

Se não gerar, faço um simples no Unity.

```
1950s round bakelite light switch with a small toggle lever, cream colored plate yellowed with age, two visible screws, one single isolated object only, nothing else in the image, straight front view, camera facing the object head-on at its mid height, the whole object centered and fully visible, orthographic, realistic proportions, worn and slightly dirty, muted colors, PBR materials, low-poly game asset, neutral grey background, soft even lighting, no shadows, no readable text, no logos
```
**Negative prompt**
```
wall, outlet, multiple switches, dimmer, hands, wires, other objects, extra props, scene, room, multiple views, split image, collage, three-quarter view, top view, modern, rocker switch, smart switch, glossy, logo, text, cropped, perspective distortion
```

---

## Texturas para o corredor e o quarto (imagens, não modelos)

Imagens quadradas, de 1024 a 2048 px, que repetem sem emenda. Eu aplico nas paredes e no piso que vou montar.

### Papel de parede

```
seamless tileable texture, faded 1970s floral wallpaper, small repeating pattern in dusty green and beige, yellowed with age, faint water stains, flat even lighting, top-down scan, no perspective, no shadows
```

### Carpete do corredor

```
seamless tileable texture, worn old hallway carpet, dark burgundy and brown geometric pattern, flattened pile, faded and stained in places, flat even lighting, top-down scan, no perspective, no shadows
```

### Assoalho do quarto

```
seamless tileable texture, old narrow wooden floorboards, dark varnish worn through in places, scratches and gaps between boards, flat even lighting, top-down scan, no perspective, no shadows
```

### Teto

```
seamless tileable texture, old plaster ceiling painted off-white, fine cracks, brownish water stains, flat even lighting, no perspective, no shadows
```
