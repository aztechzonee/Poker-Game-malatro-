using UnityEngine;

public class PlayerAIManager : MonoBehaviour
{
    public void Choose(GameStateData gameStateData, out PlayerChoose playerChoose, out int cost)
    {
        if (gameStateData.AllIsChoosed || gameStateData.currentPlayer.bet == gameStateData.playersMaxBet)
        {
            playerChoose = PlayerChoose.Check;
            cost = 0;
        }
        else if (gameStateData.currentPlayer.bet < gameStateData.playersMaxBet)
        {
            playerChoose = PlayerChoose.Call;
            cost = gameStateData.playersMaxBet - gameStateData.currentPlayer.bet;
        }
        else
        {
            playerChoose = PlayerChoose.Check;
            cost = 0;
        }
    }
}
