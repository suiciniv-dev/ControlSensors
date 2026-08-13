# 🎛️ ControlSensors HUD

Um dashboard moderno, elegante e inteligente para Windows, desenvolvido em **C# / .NET (WPF)**. Projetado para atuar como um HUD (Head-Up Display) minimalista, ele altera seu contexto e interface dinamicamente com base no que você está fazendo no computador: trabalhando, ouvindo música ou jogando.

## ✨ Recursos Dinâmicos

O **ControlSensors** não é um monitor estático. Ele observa o estado do sistema operacional e adapta sua interface automaticamente:

1. **📊 Dashboard Principal:** Exibe métricas detalhadas em tempo real de hardware (Uso e Temperatura de CPU/GPU, Memória RAM), tráfego de rede instantâneo (Download/Upload) e informações climáticas locais.
2. **🎵 Modo Mídia (Reativo):** Quando uma música ou vídeo entra em execução no Windows (Spotify, YouTube, etc.), o HUD se transforma. Ele exibe a capa do álbum, metadados da faixa, controles nativos de mídia e um **visualizador de áudio real (FFT)** animado de acordo com a batida.
3. **🎯 Modo Alto Desempenho (Gamer):** Detecta automaticamente processos em tela cheia (jogos). O painel entra em "Modo de Alerta", aplicando um layout vermelho agressivo com números gigantes focados exclusivamente em performance e termografia, garantindo leitura instantânea sem desviar o foco da gameplay.

## 🎨 Design e Interface (UI/UX)

A interface visual foi meticulosamente polida para oferecer a melhor experiência, unindo beleza e funcionalidade rápida:

* **Visual Moderno e 3D:** O dashboard utiliza design moderno com bordas sutis e sombras profundas para criar um efeito de profundidade, destacando os cards de monitoramento.
* **Tipografia e Iconografia:** Fonte aprimorada para garantir máxima legibilidade (ideal para uso em um segundo monitor), complementada por ícones emoji intuitivos (**🔥 CPU | ⚡ GPU | 💾 RAM**).
* **Badges de Status Inteligentes:** O sistema avalia a saúde térmica do seu PC em tempo real, fornecendo feedback visual imediato através de badges coloridas:
  * 🟢 **Normal:** Temperatura < 70°C
  * 🟡 **Alerta:** Temperatura entre 70°C e 84°C
  * 🔴 **Crítico:** Temperatura ≥ 85°C (com feedback visual de atenção)
* **Player de Música Refinado:** Elementos visuais do painel de mídia foram atualizados, proporcionando uma transição suave, melhor alinhamento e um visual mais limpo para os controles e capas de álbuns.
---

## 📸 Demonstração da Interface

### Visão Geral 
<img width="791" height="462" alt="Animacao8" src="https://github.com/user-attachments/assets/f8f1b2b2-e813-4d8b-ab63-66f729907edc" />

### Dashboard de Sensores (Uso Geral)
<img width="803" height="479" alt="2026-08-13 08_59_59-win-x64 – Explorador de Arquivos" src="https://github.com/user-attachments/assets/5c870539-5e52-4df9-8279-577945a27ee7" />

### Modo Mídia (Música Ativa com Visualizador)
<img width="1002" height="601" alt="image" src="https://github.com/user-attachments/assets/f4beb8b8-d7f3-42f6-b0b9-2e48704138db" />

### Modo Alto Desempenho (Jogo Detectado)
<img width="1920" height="1078" alt="2026-08-13 09_05_30-ControlSensors HUD" src="https://github.com/user-attachments/assets/87cf734f-82ed-48b0-988a-1ba5677bdd61" />

### Configurações Rápidas (Auto-Start e Clima)
<img width="802" height="479" alt="2026-08-13 09_00_47-win-x64 – Explorador de Arquivos" src="https://github.com/user-attachments/assets/60a847d9-a082-412a-85d2-3c5a93d1c0d8" />

---
