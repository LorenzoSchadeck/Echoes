# 🎬 FUNCIONALIDADES APENAS PARA DEMO - REMOVER NA VERSÃO FINAL

Este documento lista todas as funcionalidades que foram implementadas **apenas para a demo** e devem ser **removidas na versão final** do jogo.

## 📻 RadioController - Sistema de Fade Automático da Track 3

### **Localização:** `Assets/Scripts/Puzzles/Radio/RadioController.cs`

### **Funcionalidades a remover:**

#### 1. **Campos do Inspector (linhas ~210-216):**
```csharp
// REMOVER ESTE BLOCO INTEIRO:
[Header("🎬 DEMO ONLY - Fade & Reset (Track 3) - REMOVER NA VERSÃO FINAL")]
[Tooltip("Canvas com a imagem preta para fade (DEMO ONLY)")]
[SerializeField] private Canvas fadeCanvas;
[Tooltip("Imagem preta para o fade (DEMO ONLY)")]
[SerializeField] private Image fadeImage;
[Tooltip("Duração do fade para preto (DEMO ONLY)")]
[SerializeField] private float fadeDuration = 2f;
```

#### 2. **Chamada do timer na StartTrack3() (linha ~715):**
```csharp
// REMOVER ESTAS LINHAS:
// 🎬 DEMO ONLY: Inicia monitoramento para fade automático após 33 segundos
// TODO: REMOVER esta linha na versão final do jogo
StartCoroutine(MonitorTrack3FadeTimer());
```

#### 3. **Verificação na TurnOffRadio() (linhas ~890-898):**
```csharp
// REMOVER ESTE BLOCO INTEIRO:
// 🎬 DEMO ONLY: Verifica se era Track 3 tocando - faz fade e reseta cena
// TODO: REMOVER esta funcionalidade na versão final do jogo
bool wasTrack3Playing = currentState == RadioState.Track3Playing;

// E também remover este bloco mais abaixo:
// 🎬 DEMO ONLY: Se era Track 3 tocando, inicia fade e reset da cena
// TODO: REMOVER todo este bloco na versão final do jogo
if (wasTrack3Playing)
{
    if (showDebugLogs) Debug.Log("RadioController: 🎬 DEMO - Track 3 desligada! Iniciando fade out e reset da cena...");
    StartCoroutine(FadeOutAndResetScene());
    return; // Não executa o log final pois a cena será resetada
}
```

#### 4. **Região inteira do sistema de fade (linhas ~990-1070):**
```csharp
// REMOVER TODA ESTA REGIÃO:
#region 🎬 DEMO ONLY - Fade & Reset System - TODO: REMOVER NA VERSÃO FINAL

/// <summary>
/// DEMO ONLY: Monitora a Track 3 e executa fade automático após 33 segundos
/// TODO: REMOVER este método inteiro na versão final do jogo
/// </summary>
private IEnumerator MonitorTrack3FadeTimer()
{
    // ... todo o conteúdo do método
}

/// <summary>
/// DEMO ONLY: Corrotina que faz fade para preto e reseta a cena atual
/// TODO: REMOVER este método inteiro na versão final do jogo
/// </summary>
private IEnumerator FadeOutAndResetScene()
{
    // ... todo o conteúdo do método
}

/// <summary>
/// DEMO ONLY: Reseta a cena atual
/// TODO: REMOVER este método inteiro na versão final do jogo
/// </summary>
private void ResetCurrentScene()
{
    // ... todo o conteúdo do método
}

#endregion
```

### **Imports a remover (se não usados em outro lugar):**
```csharp
using UnityEngine.SceneManagement; // Se usado apenas para o reset
using UnityEngine.UI; // Se usado apenas para o fadeImage
```

### **Como identificar rapidamente:**
- Procure por comentários com `🎬 DEMO` ou `DEMO ONLY`
- Procure por `TODO: REMOVER`
- Procure por referências aos campos `fadeCanvas`, `fadeImage`, `fadeDuration`

---

## ✅ **Checklist para Remoção:**

- [ ] Remover campos do Inspector (fadeCanvas, fadeImage, fadeDuration)
- [ ] Remover chamada `StartCoroutine(MonitorTrack3FadeTimer())` 
- [ ] Remover verificação `wasTrack3Playing` e bloco de fade
- [ ] Remover região inteira `#region 🎬 DEMO ONLY - Fade & Reset System`
- [ ] Remover imports desnecessários (SceneManagement, UI)
- [ ] Testar se Track 3 funciona normalmente sem o fade automático
- [ ] Verificar se não há referências quebradas no Inspector

---

## 📝 **Notas:**
- **Data de criação:** 08/10/2025
- **Motivo:** Sistema temporário para demonstração da funcionalidade do rádio
- **Comportamento final esperado:** Track 3 deve tocar normalmente e permitir desligamento manual após 33s, sem fade automático