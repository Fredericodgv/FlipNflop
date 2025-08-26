# Flip’n Flop: um jogo educacional para construção interativa de diagramas de tempo

![Versão do Jogo](https://img.shields.io/github/v/release/Fredericodgv/JEDcircuitos?style=for-the-badge&label=Vers%C3%A3o)

Bem-vindo ao repositório do Flip’n Flop! Este documento serve como guia para o desenvolvimento e a contribuição com o projeto.

## 🎮 Sobre o Jogo

**Flip'n Flop** é um Jogo Educacional Digital (JED) do gênero plataforma, concebido para auxiliar estudantes de Computação e áreas afins na prática de um dos tópicos fundamentais de sistemas digitais: a **construção de diagramas de tempo de circuitos lógicos sequenciais**.

A proposta do jogo é transformar a natureza abstrata dos circuitos em uma experiência interativa e engajadora, alinhando os desafios do jogo com os objetivos pedagógicos da disciplina de Circuitos Digitais.

### A Mecânica

O jogador controla um personagem em um cenário que representa um diagrama de tempo. O objetivo é construir a forma de onda da saída (Q) de um determinado flip-flop (J-K, D, T, etc.) com base nos sinais de entrada e no clock, que são apresentados como parte do nível.

* **Plataformas como Níveis Lógicos:** O personagem se move entre duas plataformas principais: a superior representa o nível lógico **ALTO (1)** e a inferior, o nível lógico **BAIXO (0)**.
* **O Tempo Avança:** O movimento para a direita simboliza o avanço do tempo no diagrama.
* **Construção da Saída:** A trajetória escolhida pelo jogador ao pular e alternar entre as plataformas desenha a linha do sinal de saída Q, que deve refletir corretamente o funcionamento do circuito.
* **Desafios Pedagógicos:** Obstáculos são posicionados estrategicamente para forçar o jogador a tomar decisões baseadas no seu conhecimento teórico, tornando o aprendizado um desafio divertido.

## 🚀 Fluxo de Desenvolvimento com Git Flow

Este projeto utiliza o **[Git Flow](https://www.atlassian.com/br/git/tutorials/comparing-workflows/gitflow-workflow)** como modelo de ramificação para organizar o desenvolvimento. Ele define um fluxo claro para a criação de funcionalidades, releases e correções.

### 1. Desenvolvendo uma Nova Funcionalidade (`feature`)

Todo novo desenvolvimento deve começar a partir da branch `develop`.

**Para iniciar uma nova funcionalidade:**

```bash
# Ex: git flow feature start menu-opcoes
git flow feature start <nome-da-feature>
```
Isso criará uma nova branch feature/<nome-da-feature>. Faça seus commits nela seguindo o padrão de commits descrito na próxima seção.

**Para finalizar a funcionalidade:**

```bash
# Ex: git flow feature finish menu-opcoes
git flow feature finish <nome-da-feature>
```
Este comando fará o merge da sua branch de volta na develop e a apagará, mantendo o histórico limpo.

### 2. Preparando um Novo Release (release)

Quando a branch develop tiver funcionalidades suficientes para uma nova versão, o processo de release é iniciado.

**Passo A: Descobrir a Próxima Versão**

Antes de tudo, você precisa determinar se o release será Major, Minor ou Patch. Use o comando abaixo para que a ferramenta analise os commits na develop e sugira o tipo de incremento:

```bash
# (Requer 'npm install -g conventional-recommended-bump')
conventional-recommended-bump -p angular
```
O comando retornará patch, minor ou major. Calcule o novo número da versão a partir disso (ex: se a versão atual é 1.2.0 e o resultado foi minor, a nova versão será 1.3.0).

**Passo B: Iniciar o Release**

Use o número de versão que você acabou de determinar:

```bash
# Ex: git flow release start 1.3.0
git flow release start <nova-versao>
```
Isso cria uma branch release/<nova-versao> onde apenas pequenas correções de última hora devem ser feitas.

**Passo C: Finalizar o Release**

Este é o comando que prepara tudo para a automação:

```bash
# Ex: git flow release finish 1.3.0
git flow release finish <nova-versao>
```
O Git Flow irá, automaticamente, fazer o merge na main, criar uma tag com o número da versão, e fazer o merge de volta na develop.

### 3. Corrigindo um Bug Urgente (hotfix)

Se um bug crítico for encontrado na versão de produção (na branch main).

**Para iniciar um hotfix:**

A versão deve seguir o padrão PATCH.

```bash
# Ex: git flow hotfix start 1.3.1
git flow hotfix start <versao-hotfix>
```
Isso cria uma nova branch hotfix/<versao-hotfix> a partir da main. Faça a correção e commite.

**Para finalizar o hotfix:**

```bash
# Ex: git flow hotfix finish 1.3.1
git flow hotfix finish <versao-hotfix>
```
Este comando fará o merge da correção tanto na main quanto na develop (para que o bug não reapareça em releases futuros) e criará a tag da nova versão.

### 4. Sincronizando e Acionando a Automação

Após finalizar um release ou hotfix, envie tudo para o repositório remoto. Estes comandos irão acionar a automação.

```bash
git push origin main
git push origin develop
git push --tags
```

## 📝 Padrão de Commits (Conventional Commits)

| Tipo de Commit | O que significa                              | Impacto na Versão (Major.Minor.Patch)          |
|----------------|----------------------------------------------|------------------------------------------------|
| `feat`         | Adiciona uma nova funcionalidade ao jogo.    | Incrementa **MINOR** (`1.2.3` → `1.3.0`)       |
| `fix`          | Corrige um bug ou problema no jogo.          | Incrementa **PATCH** (`1.2.3` → `1.2.4`)       |

Este projeto utiliza Conventional Commits. Seguir este padrão é obrigatório para todos os commits. A estrutura é:

```text
<tipo>(escopo opcional): <descrição>
```

### Tipos de Commit

**Outros Tipos de Commit (Não geram nova versão)**

- `chore`: Tarefas de manutenção.
- `docs`: Mudanças na documentação.
- `style`: Mudanças de formatação de código.
- `refactor`: Refatoração de código.
- `test`: Adição ou correção de testes.
- `ci`: Mudanças nos arquivos de automação (release.yml).

### Breaking Changes (Mudanças Quebradiças)

Para alterações que quebram a compatibilidade (MAJOR), adicione um `!` após o tipo/escopo ou um rodapé `BREAKING CHANGE:`.

**Exemplo:**

```text
refactor(save)!: altera o formato do arquivo de save

BREAKING CHANGE: Saves criados em versões anteriores não são mais compatíveis.
```

## ⚙️ O Processo de Release Automatizado

Após você finalizar um release ou hotfix com o git flow e enviar as alterações para a main e as tags para o GitHub, o nosso workflow de automação (GitHub Actions) é acionado. Ele irá executar as seguintes tarefas:

1. Fazer o checkout do código da main, já com a tag da nova versão.
2. Atualizar o número da versão dentro do projeto Unity.
3. Compilar o jogo para a plataforma configurada.
4. Gerar as notas do release (changelog) com base nos seus commits.
5. Publicar um "Release" oficial na página do GitHub, anexando o jogo compilado e o changelog.
