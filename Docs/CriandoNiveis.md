[← Voltar para a página inicial](../README.md)

# 🧩 Como Criar Seus Próprios Níveis

O **Flip'n Flop** permite que qualquer pessoa crie novas fases personalizadas de forma simples usando arquivos **JSON**.

---

## 📄 Estrutura Básica do Arquivo

Para criar um novo nível, crie um arquivo de texto com a extensão `.json` (por exemplo, `meu_nivel.json`) seguindo o modelo abaixo:

```json
{
    "levelName": "nivel_exemplo",
    "levelDescription": "Fase de teste ensinando o Flip-Flop JK",
    "levelID": "102030",
    "levelOrder": 10,
    "ffType": "JK",
    "clockCicles": 6,
    "activeClockEdge": "rising",
    "jSignal": "000000 000111 111000 000000 001111 111111 111111",
    "kSignal": "000000 000000 000011 111111 111111 100011 111111",
    "floor":   "111111 111111 111111 111111 111111 111111 111111 111111 111111 111",
    "ceiling": "111111 111111 111111 111111 111111 111111 111111 111111 111111 111",
    "obstacles": []
}
```

---

## 📌 O que preencher em cada campo?

### 1. Identificação da Fase
* **`levelName`**: O nome interno do nível (use sem espaços, ex: `"primeiro_nivel"`).
* **`levelDescription`**: Uma breve descrição do objetivo ou mecânica da fase.
* **`levelID`**: Um número de identificação único para o nível (ex: `"4104892"`).
* **`levelOrder`**: A ordem em que a fase aparece no jogo (ex: `10` para a primeira, `20` para a segunda).

### 2. Configuração Lógica
* **`ffType`**: O tipo de Flip-Flop da fase (`"JK"`, `"D"`, `"T"`, etc.).
* **`clockCicles`**: Quantos ciclos completos de clock a fase terá.
* **`activeClockEdge`**: Qual borda do clock ativa o flip-flop (`"rising"` para subida, `"falling"` para descida).

### 3. Desenhando o Cenário e os Sinais
Aqui você "desenha" a fase usando sequências de números `0` e `1` (você pode usar espaços para facilitar a leitura visual):
* **Sinais (`jSignal`, `kSignal`...)**: Representam o valor da entrada em cada pedacinho do tempo. `0` é sinal baixo, `1` é sinal alto.
* **Plataformas (`floor`, `ceiling`)**: Onde o personagem pode pisar. Coloque `1` para existir chão/teto naquele ponto, ou `0` para deixar um buraco no cenário.

---

## 💥 Adicionando Obstáculos (Opcional)

Se quiser deixar a fase mais desafiadora, você pode adicionar inimigos (como a **Mace** giratória) dentro da lista `"obstacles"`. 

Veja como configurar um obstáculo com movimento:

```json
"obstacles": [
    {
        "obstacleName": "Mace",
        "startX": 9,
        "startY": 1,
        "speed": 2,
        "horizontalDistance": 6,
        "verticalDistance": 3,
        "starterCorner": "bottom-left",
        "clockwise": true
    }
]
```

### Configurando o Obstáculo:
* **`obstacleName`**: O nome do inimigo (ex: `"Mace"`).
* **Posição Inicial**: Use **`startX`** (posição na horizontal) e **`startY`** (posição na vertical) para dizer onde ele nasce.
* **`speed`**: A velocidade de movimento (`0` para ficar parado, `1` a `4` para se mover mais rápido).
* **Distância do Movimento**: Quanto ele vai patrulhar para os lados (**`horizontalDistance`**) e para cima/baixo (**`verticalDistance`**).
* **`starterCorner`**: De qual canto ele começa a se mover (`"bottom-left"`, `"bottom-right"`, `"top-left"`, `"top-right"`).
* **`clockwise`**: Escreva `true` para ele girar no sentido horário, ou `false` para o sentido anti-horário.
