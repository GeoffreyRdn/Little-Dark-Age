using UnityEngine;

public class ChangePlayerMaterial : MonoBehaviour
{
    public Material[] playerMaterials;

    private void OnEnable()
    {
        // Find all objects with the "player" tag and change their material
        GameObject[] players = GameObject.FindGameObjectsWithTag("player");
        foreach (GameObject player in players)
        {
            Renderer renderer = player.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Get a random unused material
                Material newMaterial = GetUnusedMaterial();

                // Change the material
                renderer.material = newMaterial;

                // Store the used material on the object
                player.GetComponent<PlayerMaterial>().material = newMaterial;
            }
        }
    }

    private Material GetUnusedMaterial()
    {
        // Shuffle the materials array
        Shuffle(playerMaterials);

        // Loop through the materials array and return the first unused material
        foreach (Material material in playerMaterials)
        {
            bool isUsed = false;

            // Check if the material is already used on another player object
            GameObject[] players = GameObject.FindGameObjectsWithTag("player");
            foreach (GameObject player in players)
            {
                PlayerMaterial playerMaterial = player.GetComponent<PlayerMaterial>();
                if (playerMaterial != null && playerMaterial.material == material)
                {
                    isUsed = true;
                    break;
                }
            }

            // Return the material if it is unused
            if (!isUsed)
            {
                return material;
            }
        }

        // If all materials are used, return the first material in the array
        return playerMaterials[0];
    }

    private void Shuffle(Material[] materials)
    {
        // Fisher-Yates shuffle algorithm
        for (int i = materials.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Material temp = materials[i];
            materials[i] = materials[j];
            materials[j] = temp;
        }
    }
}

public class PlayerMaterial : MonoBehaviour
{
    public Material material;
}
