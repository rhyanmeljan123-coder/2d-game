using UnityEngine;

/// <summary>Tracks the reusable watering can's water charges.</summary>
public static class WateringCanSystem
{
    public const int MaxCharges = 5;
    public static int Charges { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => Charges = MaxCharges;

    public static bool TryUse()
    {
        if (Charges <= 0) return false;
        Charges--;
        return true;
    }

    public static void Refill()
    {
        Charges = MaxCharges;
        GameHud.Toast("Watering can refilled (5/5).");
    }

    public static void Restore(int charges) => Charges = Mathf.Clamp(charges, 0, MaxCharges);
}
