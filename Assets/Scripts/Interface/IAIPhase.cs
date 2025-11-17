using System.Collections;

public interface IAIPhase
{
    void OnEnter();
    void OnExit();
    IEnumerator Execute(AIPlayer ai);
}