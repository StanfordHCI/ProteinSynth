using Yarn.Unity;

public class GlobalInMemoryVariableStorage: InMemoryVariableStorage
{
    /// Singleton
    private static GlobalInMemoryVariableStorage instance = null;

    void Awake() {
        if (instance != null) {
            Destroy(gameObject);
        } else {
            instance = this;
            // DontDestroyOnLoad(gameObject);
        }
    }

    public static GlobalInMemoryVariableStorage Instance{
      get {
        return instance;
      }
    }
}