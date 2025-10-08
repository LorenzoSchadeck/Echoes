# WARNING SCREEN - SISTEMA SIMPLES ATUALIZADO

## NOVIDADES:
- ✅ **Referências por painel**: Cada painel usa seus próprios TextMeshPro
- ✅ **Spinner no último painel**: Aparece apenas quando necessário

## COMO USAR:

### 1. CRIAR PAINÉIS:
- Right Click → Create → Echoes → Warning Panel
- Configure title e text
- **Configure Title Text Reference e Main Text Reference específicas**
- Arraste para a lista no Manager

### 2. SETUP UI:
```
Canvas
├── Panel 1 (CanvasGroup)
│   ├── Title 1 (TextMeshPro)
│   └── Text 1 (TextMeshPro)
├── Panel 2 (CanvasGroup)  
│   ├── Title 2 (TextMeshPro)
│   └── Text 2 (TextMeshPro)
├── Panel N (CanvasGroup)
│   ├── Title N (TextMeshPro)
│   └── Text N (TextMeshPro)
├── Spinner (Image + CanvasGroup + LoadingSpinner)
└── Manager (WarningScreenManager)
```

### 3. CONFIGURAR:

**WarningPanelData (cada painel):**
- Title: texto do título  
- Text: texto principal
- **Title Text Reference**: TextMeshPro específico do título
- **Main Text Reference**: TextMeshPro específico do texto

**WarningScreenManager:**
- Warning Panels: [seus ScriptableObjects]
- Panel Duration: tempo visível
- Fade Duration: tempo fade  
- Target Scene Name: cena destino
- Panel Canvas Group: CanvasGroup principal
- Spinner: referência ao LoadingSpinner

### 4. COMPORTAMENTOS:
- ✅ **Cada painel controla seus próprios textos**
- ✅ **Spinner aparece APENAS no último painel**
- ✅ **Carregamento assíncrono em background**
- ✅ **Fade independente por painel**

### 5. VANTAGENS:
- **Flexibilidade**: Painéis podem ter layouts diferentes
- **Performance**: Spinner só ativo quando necessário
- **Modularidade**: Cada painel é independente
- **Simplicidade**: Configuração direta no ScriptableObject

SISTEMA SIMPLES E PODEROSO!