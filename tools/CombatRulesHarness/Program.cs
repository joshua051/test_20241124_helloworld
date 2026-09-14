using System;
using IronSand.Combat;
using IronSand.Scoring;
using UnityEngine;

internal static class Program
{
    private static int checks;
    private static void Main()
    {
        AttackTimelineChecks();
        GuardChecks();
        PoiseChecks();
        ExecutionChecks();
        DodgeChecks();
        WeaponChecks();
        StyleChecks();
        Console.WriteLine($"CombatRulesHarness PASS ({checks} checks)");
    }

    private static void AttackTimelineChecks()
    {
        AttackProfile profile = new(0.2f, 0.1f, 0.3f, 1f, 20f, 0.3f, 0.6f, 0.05f, 0.1f);
        AttackTimeline timeline = new();
        True(timeline.TryStart(profile), "attack starts");
        Equal(CombatPhase.Startup, timeline.Phase, "startup phase");
        timeline.Tick(0.21f);
        Equal(CombatPhase.Active, timeline.Phase, "active phase");
        timeline.Tick(0.11f);
        Equal(CombatPhase.Recovery, timeline.Phase, "recovery phase");
        timeline.Tick(0.30f);
        True(!timeline.IsRunning, "attack completes");
        timeline = new AttackTimeline();
        True(timeline.TryStart(profile), "root attack starts");
        float root = 0f;
        for (int i = 0; i < 120; i++) root += timeline.Tick(1f / 120f);
        Near(1f, root, 0.0001f, "root fraction sums to one");
        AttackProfile sword = AttackLibrary.Get(WeaponArchetype.Sword, AttackKind.Heavy);
        AttackProfile mace = AttackLibrary.Get(WeaponArchetype.Mace, AttackKind.Heavy);
        AttackProfile spear = AttackLibrary.Get(WeaponArchetype.Spear, AttackKind.Light);
        True(mace.Total > sword.Total, "mace is slower than sword");
        True(mace.PoiseDamage > sword.PoiseDamage, "mace has more poise damage");
        True(spear.RootMotionDistance > AttackLibrary.Get(WeaponArchetype.Sword, AttackKind.Light).RootMotionDistance, "spear commits farther");
    }

    private static void GuardChecks()
    {
        GuardState guard = new(0.16f, 120f);
        guard.Begin(10f);
        Equal(GuardResolution.Perfect, guard.Resolve(Vector3.forward, Vector3.forward, 10.10f), "perfect guard window");
        Equal(GuardResolution.Block, guard.Resolve(Vector3.forward, Vector3.forward, 10.30f), "held guard block");
        Equal(GuardResolution.None, guard.Resolve(Vector3.forward, Vector3.back, 10.10f), "rear bypasses guard");
    }

    private static void PoiseChecks()
    {
        PoiseState poise = new(100f, 0.5f, 40f);
        True(!poise.Apply(60f), "poise not broken early");
        True(poise.Apply(40f), "poise breaks at zero");
        Near(0f, poise.Current, 0.0001f, "poise reaches zero");
        poise.Tick(0.4f);
        Near(0f, poise.Current, 0.0001f, "poise recovery delay");
        poise.Tick(0.2f);
        True(poise.Current > 0f, "poise recovers");
    }

    private static void ExecutionChecks()
    {
        ExecutionState execution = new();
        True(execution.TryStart(1f, 0.5f), "execution starts");
        int strikes = 0;
        for (int i = 0; i < 10; i++) if (execution.Tick(0.1f).StrikeNow) strikes++;
        Equal(1, strikes, "execution strike exactly once");
        True(!execution.IsActive, "execution completes");
    }

    private static void DodgeChecks()
    {
        DodgeState dodge = new();
        True(dodge.TryStart(0.3f, 0.8f), "dodge starts");
        True(!dodge.TryStart(0.3f, 0.8f), "dodge cannot restart active");
        float active = 0f;
        for (int i = 0; i < 60; i++) active += dodge.Tick(1f / 60f);
        Near(0.3f, active, 0.0001f, "dodge active time stable");
        True(dodge.CanStart, "dodge recovers");
    }

    private static void WeaponChecks()
    {
        Equal("Gladius", WeaponCatalog.Get(WeaponArchetype.Sword).DisplayName, "sword catalog");
        True(WeaponCatalog.Get(WeaponArchetype.Axe).HeavyDamageMultiplier > WeaponCatalog.Get(WeaponArchetype.Sword).HeavyDamageMultiplier, "axe heavy damage");
        Equal(WeaponArchetype.Sword, WeaponCatalog.GetArenaWeapon(0), "arena weapon seed 0");
        Equal(WeaponArchetype.Sword, WeaponCatalog.GetArenaWeapon(int.MinValue), "arena weapon handles int min");
    }

    private static void StyleChecks()
    {
        StyleScoreModel style = new(2.5f);
        StyleAward light = style.RegisterHit(AttackKind.Light, WeaponArchetype.Sword, false, 1f);
        StyleAward varied = style.RegisterHit(AttackKind.Heavy, WeaponArchetype.Axe, false, 1.5f);
        StyleAward execution = style.RegisterHit(AttackKind.Execution, WeaponArchetype.Axe, true, 2f);
        True(varied.Points > light.Points, "variety/heavy increases award");
        True(execution.Points > varied.Points, "execution reward is higher");
        True(style.ComboCount == 3, "combo increments");
        True(style.Tick(5f), "combo expires");
        Equal(0, style.ComboCount, "expired combo resets");
    }

    private static void True(bool condition, string name)
    {
        checks++;
        if (!condition) throw new InvalidOperationException($"FAILED: {name}");
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        checks++;
        if (!Equals(expected, actual)) throw new InvalidOperationException($"FAILED: {name}; expected={expected}, actual={actual}");
    }

    private static void Near(float expected, float actual, float tolerance, string name)
    {
        checks++;
        if (Math.Abs(expected - actual) > tolerance) throw new InvalidOperationException($"FAILED: {name}; expected={expected}, actual={actual}");
    }
}
