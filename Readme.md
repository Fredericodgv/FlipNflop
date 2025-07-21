# Flip’n Flop: um jogo educacional para construção interativa de diagramas de tempo

![Versão do Jogo](https://img.shields.io/github/v/release/Fredericodgv/JEDcircuitos?style=for-the-badge&label=Vers%C3%A3o)

Bem-vindo ao repositório do Flip’n Flop! Este documento serve como guia para o desenvolvimento e a contribuição com o projeto.

## 🎮 Sobre o Jogo
**Flip'n Flop** é um Jogo Educacional Digital (JED) do gênero plataforma, concebido para auxiliar estudantes de Computação e áreas afins na prática de um dos tópicos fundamentais de sistemas digitais: a construção de diagramas de tempo de circuitos lógicos sequenciais.

A proposta do jogo é transformar a natureza abstrata dos circuitos em uma experiência interativa e engajadora, alinhando os desafios do jogo com os objetivos pedagógicos da disciplina de Circuitos Digitais.

### A Mecânica
O jogador controla um personagem em um cenário que representa um diagrama de tempo. O objetivo é construir a forma de onda da saída (Q) de um determinado flip-flop (J-K, D, T, etc.) com base nos sinais de entrada e no clock, que são apresentados como parte do nível.

* **Plataformas como Níveis Lógicos:** O personagem se move entre duas plataformas principais: a superior representa o nível lógico **ALTO (1)** e a inferior, o nível lógico **BAIXO (0)**.
* **O Tempo Avança:** O movimento para a direita simboliza o avanço do tempo no diagrama.
* **Construção da Saída:** A trajetória escolhida pelo jogador ao pular e alternar entre as plataformas desenha a linha do sinal de saída Q, que deve refletir corretamente o funcionamento do circuito.
* **Desafios Pedagógicos:** Obstáculos são posicionados estrategicamente para forçar o jogador a tomar decisões baseadas no seu conhecimento teórico, tornando o aprendizado um desafio divertido.

## 🚀 Como Contribuir com Commits
Este projeto utiliza **[Conventional Commits](https://www.conventionalcommits.org/)** para automatizar o versionamento e a criação de changelogs. Seguir este padrão é **obrigatório** para todos os commits.

A estrutura de uma mensagem de commit deve ser:
```<tipo>(escopo opcional): <descrição>```

### Tipos de Commit
A parte mais importante é o **tipo**, que define o impacto da sua alteração na versão do jogo:

| Tipo de Commit | O que significa | Impacto na Versão (Major.Minor.Patch) |
| :--- | :--- | :--- |
| **`feat`** | Adiciona uma nova funcionalidade ao jogo. | Incrementa **MINOR** (`1.2.3` -> `1.3.0`) |
| **`fix`** | Corrige um bug ou problema no jogo. | Incrementa **PATCH** (`1.2.3` -> `1.2.4`) |

### Outros Tipos de Commit (Não geram nova versão)
Use os seguintes tipos para organizar o histórico sem criar um novo release:

* **`chore`**: Tarefas de manutenção (ex: organizar pastas, atualizar pacotes).
* **`docs`**: Mudanças na documentação (comentários no código, este README).
* **`style`**: Mudanças de formatação de código que não alteram a lógica.
* **`refactor`**: Refatoração de código sem adicionar funcionalidades ou corrigir bugs.
* **`test`**: Adição ou correção de testes.
* **`ci`**: Mudanças nos arquivos de automação (ex: `release.yml`).

### 💥 Breaking Changes (Mudanças Quebradiças)
Para alterações que quebram a compatibilidade e exigem um incremento de versão **MAJOR** (`1.2.3` -> `2.0.0`), adicione um `!` após o tipo/escopo ou um rodapé `BREAKING CHANGE:`.

**Exemplo:**

````
refactor(save)!: altera o formato do arquivo de save
BREAKING CHANGE: Saves criados em versões anteriores não são mais compatíveis.

`````

### ⚙️ O Processo de Release Automatizado
1.  **Faça suas alterações** em uma branch separada.
2.  **Faça o commit** das suas alterações usando as regras acima.
3.  **Abra um Pull Request** para a branch `main`.
4.  **Após o merge**, a automação (GitHub Actions) irá:
    * Analisar os commits.
    * Determinar a nova versão do jogo.
    * Atualizar a versão dentro do projeto Unity.
    * Fazer o build do jogo.
    * Criar um novo Release no GitHub com o jogo para download e as notas da versão.

> **Nota:** Para fazer um commit que **não** deve acionar a automação de forma alguma, inclua `[skip ci]` na sua mensagem de commit.
