using System;
using Godot;



[GlobalClass]
public partial class GODartGun : BasicGun
{
    [Export] public override InventoryGroupCategory category { get; set; } = InventoryGroupCategory.Role;
    public override void OnPickup(ulong bySteamID)
    {
        Logging.Log(bySteamID + " Just Picked a " + category.ToString() + " Up" + $"({id})", "GOBaseInventoryItem");
        Freeze = true;
        this.CollisionLayer = 0;
        firstPersonScene.Hide();
        thirdPersonScene.Hide();
        inInventoryOf = bySteamID;
        if(inInventoryOf == Global.steamid)
        {
            Global.ui.inGameUI.PlayerUIManager.UpdateInventorySlot(4, iconPath);
        }
    }

    
    public override void OnDropped(ulong bySteamID)
    {
        Logging.Log(bySteamID + " Just Dropped a " + category.ToString()+ $"({id})", "GOBaseInventoryItem");
        firstPersonScene.Hide();
        thirdPersonScene.Show();
        this.CollisionLayer = 1 << 3;
        Freeze = false;
        equippedBySteamID = 0;
        inInventoryOf = 0;
        if (currentParent != null)
        {
            DetachFromPlayer(currentParent);
        }
        if(inInventoryOf == Global.steamid)
        {
            Global.ui.inGameUI.PlayerUIManager.UpdateInventorySlot(4,"");
            Global.ui.inGameUI.PlayerUIManager.UpdateAmmoUI(0, 0, 0);
        }
        //reset reload progress
        reloading = false;
        reloadTimeLeft = reloadTimeSeconds;
        audioStreamPlayer1.Stop();
        audioStreamPlayer2.Stop();
        animationPlayer.Play("RESET");
    }
}