/// Whatever a KeypadUI checks a 4-digit guess against. CodeManager (Room 1's clue code) is the
/// first implementer; Room 2's access keypad (Stage 2) validates against a different source
/// without KeypadUI needing to know the difference.
public interface IKeypadValidator
{
    bool Validate(int[] guess);
}
