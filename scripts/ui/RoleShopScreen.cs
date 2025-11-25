using Godot;
using Steamworks;


[GlobalClass]
public partial class RoleShopScreen : PanelContainer
{
    [Export] public RoleShopScreen roleShopScreen;
    [Export] public Label creditLabel;
    [Export] public HFlowContainer itemButtonContainer;
    [Export] public Label itemDescriptionLabel;
    [Export] public Button closeWindowButton;
    [Export] public Button purchaseButton;

    [Export] public Button radarButton;
    [Export] public Button c4Button;

    private GameObjectType currentGameObjectType = GameObjectType.ERROR;



    public override void _Ready()
    {
        base._Ready();
        closeWindowButton.Pressed += CloseRoleShopScreen;
        purchaseButton.Pressed += PurchaseSelectedItem;
        radarButton.Pressed += () => SelectItem(GameObjectType.PlayerRadar);  
        c4Button.Pressed += () => SelectItem(GameObjectType.C4);      
    }

    public void OpenRoleShopScreen()
    {
        if (Global.gameState.GameObjects[Global.gameState.PlayerIDToControlledCharacter[Global.steamid]] is BasicPlayerCharacter basicPlayerCharacter)
        {
            creditLabel.Text = $"You Currently Have {basicPlayerCharacter.roleCredits} Credits";
            Input.MouseMode = Input.MouseModeEnum.Confined;
        }
        else
        {
            creditLabel.Text = $"You Arent a BasicPlayerCharacter?";
        }
        roleShopScreen.Visible = true;
    }

    public void CloseRoleShopScreen()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        roleShopScreen.Visible = false;
    }    

    public void SelectItem(GameObjectType gameObjectType)
    {
        currentGameObjectType = gameObjectType;
        if (gameObjectType == GameObjectType.C4)
        {
            itemDescriptionLabel.Text = "C4:\nC4 is planted by left clicking, it will explode in a large radius hurting and killing players through walls.";
        }
        else if(gameObjectType == GameObjectType.PlayerRadar)
        {
            itemDescriptionLabel.Text = "Radar:\nRadar will periodically (and automatically) mark the location of all the workers on the map, tracking their location for a short duration.";
        }
    }

    public void PurchaseSelectedItem()
    {
        if(currentGameObjectType != GameObjectType.ERROR)
        {
            //spawn and pickup the item (if we can)
            if (Global.gameState.GameObjects[Global.gameState.PlayerIDToControlledCharacter[Global.steamid]] is BasicPlayerCharacter basicPlayerCharacter)
            {
                if(basicPlayerCharacter.roleCredits > 0)
                {
                    Transform3D spawnTransform = basicPlayerCharacter.Transform;
                    spawnTransform.Origin += new Vector3(0, 2, -1);
                    GameObjectConstructorData data = new(currentGameObjectType);
                    data.spawnTransform = spawnTransform;
                    data.paramList.Add(true);
                    data.paramList.Add(Global.steamid);
                    Global.gameState.Auth_SpawnObject(currentGameObjectType, data);
                    basicPlayerCharacter.roleCredits--;
                    creditLabel.Text = $"You Currently Have {basicPlayerCharacter.roleCredits} Credits";
                    CloseRoleShopScreen();
                }
            }
            else
            {
                Logging.Error("The current role shopper isnt a basicplayercharacter", "RoleShopScreen");
            }
        }
    }
}