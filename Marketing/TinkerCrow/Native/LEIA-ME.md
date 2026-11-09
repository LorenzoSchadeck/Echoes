# Exportação original da Tinker Crow

- `tinker-crow-wallpaper-vertical-2160x3840.png`: wallpaper 9:16 composto a partir do quadro final capturado. Corvo, chave, título e área central preservam os pixels originais. O fundo de papel foi estendido acima e abaixo com uma amostra do próprio fundo, suavizada para a transição. Não é uma nova captura vertical da cena.

Estes são os arquivos que atendem à solicitação de usar exatamente a arte presente no jogo. Substituem os materiais recriados da pasta superior.

- `tinker-crow-abertura-original-4k.mp4`: captura da execução de `Assets/Scenes/PreLoad.unity` pelo Unity 6000.6.7f1, utilizando o componente `StudioIntro` existente. Resolução de 3840 × 2160, aproximadamente 6,33 segundos. Vídeo H.264, áudio AAC.
- `tinker-crow-frame-final-4k.png`: quadro original sem perdas, com o nome completo, o corvo pousado e a chave, antes do escurecimento final. Resolução de 3840 × 2160. Sem geração de imagem, redesenho ou recorte.

O painel UI Toolkit foi renderizado pelo próprio Unity em uma textura de saída 4K. Preservam-se os sprites, fonte, posicionamento, papel, hachuras, granulação e animação. A versão atual inclui o ajuste solicitado no próprio jogo: um recorte do sprite original do bico fica na frente da chave metálica, e o chaveiro vermelho permanece à frente do bico. As camadas acompanham os quadros e o balanço do acessório. O áudio foi capturado da saída do mixer FMOD do jogo. A inicialização anterior à abertura foi removida e o som da digitação foi sincronizado ao primeiro caractere visível.

Os instantes efetivos de captura foram mantidos na montagem. A entrega em 30 fps repete quadros quando necessário; não sintetiza nem interpola imagens entre quadros capturados. O quadro PNG mantém os pixels da captura sem compressão com perdas, enquanto o MP4 usa compressão de alta qualidade.

`capture.json` e `export-details.json` registram os dados da captura. Os arquivos `native-frames.mkv`, `frame-*.png` e `native-timing.ffconcat` são intermediários de trabalho. Os scripts de captura são exclusivos do Editor e não alteram o componente original nem as cenas do jogo.
