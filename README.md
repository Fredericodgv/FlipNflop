# Flip’n Flop: um jogo educacional para construção interativa de diagramas de tempo

![Versão do Jogo](https://img.shields.io/github/v/release/fredericodgv/flipnflop?style=for-the-badge&label=Vers%C3%A3o)

Bem-vindo ao repositório do **Flip’n Flop**! Este documento serve como guia para o desenvolvimento, organização e contribuição com o projeto.

## 🎮 Sobre o Jogo

**Flip'n Flop** é um Jogo Educacional Digital (JED) do gênero plataforma, concebido para auxiliar estudantes de Computação e áreas afins na prática de um dos tópicos fundamentais de sistemas digitais: a **construção de diagramas de tempo de circuitos lógicos sequenciais**.

A proposta do jogo é transformar a natureza abstrata dos circuitos em uma experiência interativa e engajadora, alinhando os desafios do jogo com os objetivos pedagógicos da disciplina de Circuitos Digitais.

### A Mecânica

O jogador controla um personagem em um cenário que representa um diagrama de tempo. O objetivo é construir a forma de onda da saída (Q) de um determinado flip-flop (J-K, D, T, etc.) com base nos sinais de entrada e no clock.

* **Plataformas como Níveis Lógicos:** O personagem se move entre duas plataformas: a superior representa o nível lógico **ALTO (1)** e a inferior, o nível lógico **BAIXO (0)**.
* **O Tempo Avança:** O movimento para a direita simboliza o avanço do tempo no diagrama.
* **Construção da Saída:** A trajetória escolhida pelo jogador desenha a linha do sinal de saída Q.
* **Desafios Pedagógicos:** Obstáculos são posicionados para forçar decisões baseadas no conhecimento teórico (bordas de subida/descida, estados de memória, etc).

---

## 📂 Organização do Projeto

A lógica do jogo está concentrada na pasta `Assets/Scripts`, seguindo uma estrutura modular para facilitar a manutenção:

* **`Hint/`**: Controladores de auxílio visual e linhas de guia para o clock (`ClockLineHint.cs`).
* **`Level/`**: O "cérebro" lógico. Contém o simulador de flip-flops (`FlipFlopSimulator.cs`) e o carregador de níveis via JSON.
* **`Player/`**: Scripts de movimentação e o verificador de caminho (`PathVerifier.cs`), que valida se o traçado do jogador está correto.
* **`UI/`**: Gerenciamento de menus, pausa e interface de usuário.

---

## 📚 Documentação Adicional

Para manter este README limpo, detalhes técnicos específicos e histórico foram movidos para páginas dedicadas:

* 🚀 [**Visão Geral e Arquitetura do Projeto**](Docs/VisaoGeralDoProjeto.md): Guia completo da arquitetura de código, módulos C#, fluxo de execução e conceitos do jogo para novos desenvolvedores.
* 📖 [**Guia de Versionamento**](Docs/Versionamento.md): Padrões de commits e regras para branches.
* 📜 [**Changelog**](CHANGELOG.md): Histórico detalhado de todas as versões e alterações.
* 🧩 [**Criação de Níveis**](Docs/CriandoNiveis.md): Instruções para criar novos níveis usando arquivos JSON.

---

## 🚀 Fluxo de Desenvolvimento (Git Flow)

Este projeto utiliza o **[Git Flow](https://www.atlassian.com/br/git/tutorials/comparing-workflows/gitflow-workflow)** para organizar o desenvolvimento.

### 1. Branches Principais
* `main`: Contém o código em estado de produção (estável).
* `develop`: Branch principal de desenvolvimento onde as funcionalidades são integradas.

### 2. Desenvolvendo uma Nova Funcionalidade (`feature`)
Toda nova funcionalidade deve ser criada a partir da branch `develop`:

```bash
git checkout develop
git checkout -b feature/nome-da-funcionalidade
```

### 3. Finalizando uma Funcionalidade
Após concluir e testar, a feature é integrada de volta à `develop`:

```bash
git checkout develop
git merge --no-ff feature/nome-da-funcionalidade
git branch -d feature/nome-da-funcionalidade
```

---

## 🛠️ Tecnologias Utilizadas
* **Engine:** Unity
* **Linguagem:** C#
* **Formato de Dados:** JSON (para criação de níveis customizados)
* **Versionamento:** Git (Git Flow)
