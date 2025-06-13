# ABX Mock Exchange TCP Client

This is a C# console client for interacting with the ABX Mock Exchange Server. The client allows you to request all order book packets or a specific packet by sequence number, as per the ABX server specification.

---

## Features

- Connects to the ABX mock exchange server over TCP.
- Supports two request types:
  - **Stream all packets** (with automatic recovery of missing packets).
  - **Request a specific packet by sequence number**.
- Displays all received order book packets in a readable format.
- Handles network errors and validates received data.

---

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (version 6.0 or later recommended)
- [Node.js](https://nodejs.org/) (for running the ABX server)
- The ABX server code (e.g., `main.js`)

---

## Setup Instructions

### 1. Clone or Download the Project

Clone or download the project.

### 2. Start the ABX Mock Exchange Server

Open a terminal in the directory containing `main.js` directory.
   Run the server:
   ```
   node main.js
   ```
   You should see:
   ```
   TCP server started on port 3000.
   ```
---


### 3. C# Client

Open a terminal in the AbxClientApp folder.
   Build:
   ```
   dotnet build
   ```
   Run:
   ```
   dotnet run
   ```
---

## Usage & Input Options

When you run the client, you will see:

```
Select request type:
0. Exit
1. Stream all packets
2. Resend packet by sequence number
Enter 1 or 2:
```

- **Option 1:**  
  Requests all available packets from the server.  
  The client will automatically detect and request any missing packets (except the last, which is never missed), ensuring you receive the complete order book.

- **Option 2:**  
  Prompts you to enter a sequence number (1-255).  
  The client will request and display only the packet with that sequence number.

- **Option 0:**  
  Exits the client.

---

## Example Output

```
Select request type:
0. Exit
1. Stream all packets
2. Resend packet by sequence number
Enter 1 or 2: 1
Seq:1 AAPL B Qty:50 Price:100
Seq:2 AAPL B Qty:30 Price:98
Seq:3 AAPL S Qty:20 Price:101
...
```

---

## Troubleshooting

- **Error: "No connection could be made because the target machine actively refused it."**
  - Ensure the ABX server is running on `localhost:3000`.
  - Check firewall settings.

- **No output or missing packets**
  - The client will automatically request missing packets (except the last). If you still see issues, ensure the server is running correctly.

---
