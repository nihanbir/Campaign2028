
using TMPro;

public class UnassignedPlayerDisplayCard : DisplayCard
{
    public TextMeshProUGUI playerID;

    public void SetUnassignedPlayerCard(string playerID)
    {
        this.playerID.text = "Player " + playerID;
    }
}
