using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Travas de movimento do jogador. Cada sistema que precisa travar o jogador (inspeção, rádio,
/// flashback, menu, morte...) adquire a trava com uma chave própria e a libera quando termina.
/// O jogador só se move quando nenhuma trava está ativa, então um sistema não destrava o
/// jogador enquanto outro ainda precisa dele parado.
/// </summary>
public static class PlayerControlLock
{
    private static readonly HashSet<object> owners = new HashSet<object>();

    public static bool IsLocked => owners.Count > 0;

    public static void Acquire(object owner) => owners.Add(owner);

    public static void Release(object owner) => owners.Remove(owner);

    public static void Set(object owner, bool locked)
    {
        if (locked) Acquire(owner);
        else Release(owner);
    }

    /// <summary>
    /// Remove todas as travas. Chamado automaticamente ao carregar uma cena.
    /// </summary>
    public static void ReleaseAll() => owners.Clear();

    // Garante estado limpo mesmo com "Enter Play Mode Options" sem domain reload
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        owners.Clear();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) owners.Clear();
    }
}
