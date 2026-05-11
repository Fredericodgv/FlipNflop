[← Voltar para a página inicial](../README.md)

# 🧩 Como criar novos níveis (JSON)

O jogo carrega as configurações de clock e portas lógicas a partir de arquivos JSON. Abaixo está a estrutura padrão:

```json
{
  "levelName": "Flip-Flop JK - Nível 1",
  "flipFlopType": "JK",
  "initialState": 0,
  "inputSignals": {
    "J": [0, 1, 1, 0],
    "K": [0, 0, 1, 1]
  }
}