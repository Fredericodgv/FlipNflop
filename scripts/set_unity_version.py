# scripts/set_unity_version.py
import sys
import re

def update_version(version):
    """
    Atualiza a 'bundleVersion' no arquivo ProjectSettings.asset da Unity.
    """
    settings_file = "ProjectSettings/ProjectSettings.asset"
    
    try:
        with open(settings_file, "r") as f:
            content = f.read()

        # AQUI ESTÁ A CORREÇÃO FINAL: Usamos uma função lambda para a substituição.
        # Isso evita qualquer ambiguidade com a string da nova versão.
        # 'm.group(1)' se refere ao primeiro grupo capturado (a parte 'bundleVersion: ').
        new_content, count = re.subn(
            r"(\bbundleVersion:\s*).*",
            lambda m: m.group(1) + version,
            content
        )

        if count == 0:
            print(f"Erro: 'bundleVersion' não encontrada no arquivo {settings_file}.")
            sys.exit(1)

        with open(settings_file, "w") as f:
            f.write(new_content)

        print(f"Versão do projeto Unity atualizada para {version} com sucesso.")

    except FileNotFoundError:
        print(f"Erro: Arquivo {settings_file} não encontrado.")
        sys.exit(1)
    except Exception as e:
        print(f"Ocorreu um erro: {e}")
        sys.exit(1)

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Uso: python set_unity_version.py <nova_versao>")
        sys.exit(1)
    
    new_version = sys.argv[1]
    update_version(new_version)