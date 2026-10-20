using System;

public enum KaelDecision
{
    None,
    OpenDoor
}

// Kaynak, karar olayını yalnızca geçerli model yanıtının doğrulanmasından sonra yayınlar.
public interface IKaelDecisionSource
{
    event Action<KaelDecision> DecisionValidated;
}
