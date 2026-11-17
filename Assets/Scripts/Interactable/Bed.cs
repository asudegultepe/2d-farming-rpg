using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Right click to sleep until the next morning: the screen fades out, the clock
// skips ahead, energy refills and the game is saved.
public class Bed : Interactable
{
    bool sleeping;

    public override void Interact(Player player)
    {
        if (sleeping) { return; }
        StartCoroutine(Sleep());
    }

    IEnumerator Sleep()
    {
        sleeping = true;

        ScreenTint screenTint = GameSceneManager.instance.ScreenTint;
        screenTint.Tint();
        // wait until the screen is fully dark, ScreenTint fades back in by itself
        yield return new WaitForSeconds(1f / screenTint.speed);

        GameManager.instance.energy.Refill();
        GameManager.instance.timeController.SkipToMorning();
        // the day change saves too, but sleeping after midnight doesn't change the day
        SaveManager.instance.Save();

        Notification.Show("Good morning!");
        sleeping = false;
    }
}
