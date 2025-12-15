using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SimpleWheel : MonoBehaviour
{
    // Referencias a objetos de la interfaz
    [Header("Referencias")]
    public Transform wheel;         
    public Button spinButton;    // Boton para iniciar el giro
    public TMP_Text rewardText;     // Texto donde se muestra el premio

    // Lista de sectores de la ruleta
    [Header("Opciones de la ruleta")]
    public string[] premios;      // Lista de tipos de premio
    public float spinDuration = 4f; // Duracion giro

    [Range(0, 11)]
    public int offset = 0;       // Desfase para ajustar la posicion del selector!!!!!!

    // Listas de objetos que pueden salir como premio
    [Header("Listas de objetos con valor")]
    public ItemConValor[] comidas;
    public ItemConValor[] muebles;

    // Penalizacion si el resultado es un fallo
    [Header("Penalizacion por fallar")]
    public int corduraPerdidaPorFallo = 10;

    private bool isSpinning = false;
    private float currentAngle = 0f; 


    void Awake()
    {
        if (spinButton != null)
        {
            spinButton.onClick.RemoveAllListeners();
            spinButton.onClick.AddListener(SpinWheel);
            spinButton.interactable = true;
        }
    }

    public void SpinWheel()
    {
        if (!isSpinning && wheel != null && premios.Length > 0)
            StartCoroutine(Spin());
    }

    IEnumerator Spin()
    {
        isSpinning = true;
        spinButton.interactable = false;
        float anglePerSector = 360f / premios.Length;

        int randomSector = Random.Range(0, premios.Length);
        float randomAngle = randomSector * anglePerSector;
        float totalRotation = 360f * 5f + randomAngle;
        float elapsed = 0f;
        float startAngle = currentAngle;

        // Animacion progresiva del giro con suavizado
        while (elapsed < spinDuration)
        {
            float t = elapsed / spinDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            currentAngle = startAngle + totalRotation * t;
            wheel.eulerAngles = new Vector3(0, 0, currentAngle);

            elapsed += Time.deltaTime;
            yield return null;
        }
        currentAngle %= 360f;
        if (currentAngle < 0) currentAngle += 360f;

        // Corrige para apuntar hacia arriba
        float correctedAngle = 360f - currentAngle;
        correctedAngle %= 360f;

        // Determina el sector ganador
        int sectorIndex = Mathf.FloorToInt(correctedAngle / anglePerSector);
        sectorIndex = (sectorIndex + offset + premios.Length) % premios.Length;

        //Premios
        string tipoPremio = premios[sectorIndex];
        string premioFinal = "";
        int dineroGanado = 0;
        bool esFallo = false;

        switch (tipoPremio)
        {
            case "Dinero":
                dineroGanado = Random.Range(5, 1001);
                premioFinal = dineroGanado + " monedas";
                break;

            case "Comida":
                ItemConValor comida = comidas[Random.Range(0, comidas.Length)];
                dineroGanado = comida.valor;
                premioFinal = comida.nombre + " (+" + comida.valor + " monedas)";

                // Se agrega la comida a la nevera
                Nevera.Instance.AgregarComida(comida.nombre, 1);
                Debug.Log("Comida enviada a la nevera: " + comida.nombre);
                break;

            case "Mueble":
                ItemConValor mueble = muebles[Random.Range(0, muebles.Length)];
                dineroGanado = mueble.valor;
                premioFinal = mueble.nombre + " (+" + mueble.valor + " monedas)";
                break;

            case "Fallo":
                esFallo = true;
                premioFinal = "Sin premio";
                break;

            default:
                premioFinal = tipoPremio;
                break;
        }

        // Penalizacion
        if (esFallo)
        {
            GameManager.Instance.cordura -= corduraPerdidaPorFallo;
            if (GameManager.Instance.cordura < 0)
                GameManager.Instance.cordura = 0;

            Debug.Log("Fallo. Se perdio " + corduraPerdidaPorFallo + " de cordura.");
        }
        else if (dineroGanado > 0)
        {
            GameManager.Instance.GanarDinero(dineroGanado);
            Debug.Log("Ganaste " + dineroGanado + " monedas");
        }

        GameManager.Instance.OnEstadisticasActualizadas?.Invoke();     // Actualiza la UI mediante el evento

        if (rewardText != null)          // Muestra el texto del premio
            rewardText.text = "Recompensa: " + premioFinal;

        spinButton.interactable = true;
        isSpinning = false;
    }
}
