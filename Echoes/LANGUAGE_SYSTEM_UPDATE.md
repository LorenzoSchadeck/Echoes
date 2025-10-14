# Sistema de Detecção Automática de Idioma

## 🎯 **Implementação Realizada**

O sistema de localização do **Echoes** foi atualizado para automaticamente detectar e usar o idioma do sistema operacional como padrão, mantendo a funcionalidade de permitir que o usuário mude o idioma posteriormente.

## 🔧 **Mudanças Implementadas**

### **1. Detecção Automática do Idioma do Sistema**
- **Arquivo**: `Assets/Scripts/UI/GameSettings.cs`
- **Método**: `DetectSystemLanguage()`
- **Funcionalidade**: 
  - Usa `Application.systemLanguage` para detectar o idioma do SO
  - Mapeia idiomas do sistema para códigos de locale suportados
  - Fallback inteligente caso o idioma do sistema não seja suportado

### **2. Idiomas Suportados no Mapeamento**
- **Português**: `pt-BR` (Portuguese Brazil)
- **Inglês**: `en` (English)
- **Espanhol**: `es` (Spanish)
- **Francês**: `fr` (French)
- **Alemão**: `de` (German)
- **Italiano**: `it` (Italian)
- **Japonês**: `ja` (Japanese)
- **Coreano**: `ko` (Korean)
- **Chinês Simplificado**: `zh-CN`
- **Chinês Tradicional**: `zh-TW`
- **Russo**: `ru` (Russian)

### **3. Lógica de Fallback Inteligente**
1. **Primeira Opção**: Idioma detectado do sistema operacional
2. **Segunda Opção**: Resultado do `SystemLocaleSelector` do Unity Localization
3. **Terceira Opção**: Inglês como último recurso

### **4. Comportamento do Sistema**

#### **Primeiro Acesso (Sem Preferência Salva)**
- Detecta automaticamente o idioma do sistema operacional
- Aplica o idioma detectado
- Salva a preferência nos `PlayerPrefs`

#### **Acessos Subsequentes**
- Carrega o idioma salvo nas preferências do usuário
- Respeita a escolha manual do usuário

#### **Reset de Configurações**
- `ResetToDefaults()`: Reseta para o idioma do sistema (não mais hardcoded inglês)
- `ResetLanguageToSystemDefault()`: Método específico para voltar ao padrão do sistema

## 🛠️ **Métodos Adicionados/Modificados**

### **DetectSystemLanguage()**
```csharp
private string DetectSystemLanguage()
```
- Detecta o idioma do sistema operacional
- Mapeia para códigos de locale suportados
- Implementa lógica de fallback inteligente

### **InitializeLanguageWhenReady()**
```csharp
private System.Collections.IEnumerator InitializeLanguageWhenReady()
```
- **ANTES**: Forçava inglês como padrão
- **DEPOIS**: Usa detecção automática do sistema

### **LoadSettings()**
```csharp
public void LoadSettings()
```
- **ANTES**: Usava inglês como fallback hardcoded
- **DEPOIS**: Usa detecção do sistema quando não há preferência salva

### **ResetToDefaults()**
```csharp
public void ResetToDefaults()
```
- **ANTES**: Resetava para inglês hardcoded
- **DEPOIS**: Reseta para o idioma do sistema operacional

### **ResetLanguageToSystemDefault()** *(Novo)*
```csharp
[ContextMenu("Reset Language to System Default")]
public void ResetLanguageToSystemDefault()
```
- Força re-detecção do idioma do sistema
- Útil para testes e quando usuário quer voltar ao padrão

### **DebugLanguageSystem()** *(Atualizado)*
```csharp
[ContextMenu("Debug Language System")]
public void DebugLanguageSystem()
```
- Agora mostra informações sobre detecção do sistema
- Exibe o idioma detectado vs. o idioma atual
- Mostra se há preferência salva

## 🎮 **Comportamento no Unity Editor**

### **Menu de Contexto Disponível**:
- **Debug Language System**: Exibe informações detalhadas do sistema de idiomas
- **Reset Language to System Default**: Força re-detecção do idioma do sistema

### **Configuração do Unity Localization**
O arquivo `Localization Settings.asset` já possui:
- `SystemLocaleSelector` configurado
- `CommandLineLocaleSelector` para override via linha de comando
- `SpecificLocaleSelector` como fallback final

## 🔍 **Debug e Logs**

O sistema agora produz logs mais informativos:

```
[GameSettings] System language detected: Portuguese
[GameSettings] Using Portuguese (Brazil) based on system locale
[GameSettings] Language system initialized with: pt-BR (Português)
```

## ✅ **Compatibilidade**

- **Compatível com `SimpleLanguageSwitcher`** existente
- **Não quebra** configurações existentes de usuários
- **Preserva** escolhas manuais de idioma
- **Funciona** com todos os sistemas de localização existentes

## 🎯 **Resultado Final**

Agora o jogo:
1. **Detecta automaticamente** o idioma do sistema operacional no primeiro acesso
2. **Aplica o idioma apropriado** baseado na detecção
3. **Permite que o usuário mude** o idioma posteriormente
4. **Preserva a escolha** do usuário em acessos futuros
5. **Oferece métodos de debug** para desenvolvimento

O sistema mantém toda a funcionalidade existente enquanto melhora significativamente a experiência do usuário ao usar automaticamente seu idioma nativo.