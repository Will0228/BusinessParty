using System;
using MixVerse.Game;
using MixVerse.Game.Kart;
using MixVerse.Game.Model.Kart;
using UnityEngine;

public static class InspectDownhillCourse
{
    public static string Preview(float fraction = 0.12f)
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode before previewing the course.");
        var view = UnityEngine.Object.FindFirstObjectByType<GameView>(FindObjectsInactive.Include);
        var settings = view != null ? view.Settings : new KartRaceSettings();
        var stage = UnityEngine.Object.FindFirstObjectByType<KartStageView>();
        if (stage == null) stage = new KartStageFactory().Create(settings, null);
        var race = new KartRace(settings, 1116);
        race.Player.Distance = settings.courseLength * fraction;
        race.Boss.Distance = race.Player.Distance + 12f;
        race.Junior.Distance = race.Player.Distance + 6f;
        stage.ResetObjects();
        stage.Render(race, 0f, 0f, false);
        var closest = float.MaxValue;
        var first = 0;
        var second = 0;
        for (var i = 0; i < stage.Path.Count; i++)
        {
            for (var j = i + 13; j < stage.Path.Count; j++)
            {
                var offset = stage.Path[j] - stage.Path[i];
                offset.y = 0f;
                if (offset.magnitude >= closest) continue;
                closest = offset.magnitude;
                first = i;
                second = j;
            }
        }
        return $"Length={settings.courseLength}, drop={stage.Point(0f).y - stage.Point(settings.courseLength).y:F2}, " +
            $"closest horizontal road centers={closest:F2}, vertical separation={Mathf.Abs(stage.Path[first].y - stage.Path[second].y):F2} " +
            $"at {first * 5}/{second * 5}m, preview={fraction:P0}";
    }
}
