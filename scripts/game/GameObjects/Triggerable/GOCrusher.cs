using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class GOCrusher : GOTrap
{
    [Export] Area3D PackagePressArea;
    [Export] Marker3D PackageOutputMarker;
    public bool AttemptLabeling()
    {
        if (Global.Lobby.bIsLobbyHost)
        {
            // Get all overlapping bodies in the Area3D
            var bodies = PackagePressArea.GetOverlappingBodies();
            // Filter to only GOPackageItem
            GOPackageBox foundBox = null;
            GOLabelPaper foundLabel = null;

            foreach (var body in bodies)
            {
                if (body is GOPackageBox box)
                {
                    foundBox = box;
                }
                if (body is GOLabelPaper label)
                {
                    foundLabel = label;
                }
            }

            // If both are present
            if (foundBox != null && foundLabel != null)
            {
                Logging.Log("Found Box is for Order: " + foundBox.orderNumber + " Found Label is for Order: " + foundLabel.orderNumber, "GOCrusher");
                // Check if they belong to the same order
                if (foundBox.orderNumber == foundLabel.orderNumber)
                {
                    Logging.Log($"✅ Label {foundLabel.orderNumber} applied to Box {foundBox.orderNumber}", "GOCrusher");

                    // Remove the label from the scene
                    RPCManager.RPC(this, "HideAppliedLabel", [foundLabel.id]);

                    // Call a function on the box (e.g. ApplyLabel)
                    RPCManager.RPC(foundBox, "ApplyLabel", []);
                    Global.gameState.gameModeManager.OrderLabelled(foundBox.orderNumber);
                    return true;
                }
            }

            // No box and label matched
            Logging.Log("NOTHING APPLIED", "GOCrusher");
            return false;
        }
        return false;
    }
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void HideAppliedLabel(ulong labelID)
    {
            GOLabelPaper item = (GOLabelPaper)Global.gameState.GameObjects[labelID];
            item.Visible = false;
            item.collider.Disabled = true;   
    }

}
