#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
================================================================================
LAN CONTROL SUITE v2.2.0 - CENTRAL MULTI-VLAN RELAY GATEWAY (LINUX & MACOS)
Tương thích 100% giao thức nhị phân (0x4E 0x53) với Windows/Linux/macOS Agents
và Windows Master Control Console.
Hỗ trợ:
  - Lắng nghe kết nối đảo chiều (Reverse TCP Tunnel) trên cổng 8443 (mặc định)
  - Chuyển tiếp hình ảnh Thumbnail, Live Remote Desktop, Chuột & Bàn phím
  - Chuyển tiếp Phát sóng màn hình 1-nhiều (Toàn bộ / Theo VLAN / Đa VLAN `AGENTS:`)
  - Chuyển tiếp Lệnh CMD/PowerShell/Bash, Kiểm kê phần cứng & Truyền tệp 2 chiều
================================================================================
"""

import os
import sys
import json
import time
import uuid
import socket
import struct
import threading
from datetime import datetime

MAGIC1 = 0x4E
MAGIC2 = 0x53
HEADER_SIZE = 11

class PacketType:
    Unknown = 0
    Register = 1
    Heartbeat = 2
    AgentList = 3
    ScreenFrame = 4
    InputEvent = 5
    CommandRequest = 6
    CommandResponse = 7
    BroadcastFrame = 8
    StartScreenStream = 9
    StopScreenStream = 10
    StartBroadcast = 11
    StopBroadcast = 12
    ScanSubnetRequest = 13
    ScanSubnetResponse = 14
    Disconnect = 15
    HelpRequest = 16
    HelpResponse = 17
    FileTransferRequest = 18
    FileTransferChunk = 19
    FileTransferResponse = 20
    FileListRequest = 21
    FileListResponse = 22


def encode_packet(packet_type: int, payload_obj=None, raw_bytes: bytes = None) -> bytes:
    if payload_obj is not None:
        if isinstance(payload_obj, str):
            json_bytes = payload_obj.encode("utf-8")
        else:
            json_bytes = json.dumps(payload_obj, ensure_ascii=False).encode("utf-8")
    else:
        json_bytes = b""
    raw_bytes = raw_bytes or b""
    header = struct.pack("<BBBii", MAGIC1, MAGIC2, packet_type & 0xFF, len(json_bytes), len(raw_bytes))
    return header + json_bytes + raw_bytes


def read_exact(sock: socket.socket, count: int) -> bytes:
    buf = bytearray()
    while len(buf) < count:
        chunk = sock.recv(count - len(buf))
        if not chunk:
            raise ConnectionError("Socket closed.")
        buf.extend(chunk)
    return bytes(buf)


def decode_packet(sock: socket.socket):
    header = read_exact(sock, HEADER_SIZE)
    m1, m2, ptype, json_len, raw_len = struct.unpack("<BBBii", header)
    if m1 != MAGIC1 or m2 != MAGIC2:
        raise ValueError("Invalid magic packet header")
    json_str = read_exact(sock, json_len).decode("utf-8", errors="replace") if json_len > 0 else ""
    raw_bytes = read_exact(sock, raw_len) if raw_len > 0 else None
    return ptype, json_str, raw_bytes, header + json_str.encode("utf-8") + (raw_bytes or b"")


class ClientSession:
    def __init__(self, sock: socket.socket, addr):
        self.sock = sock
        self.addr = addr
        self.client_type = None  # "agent" | "control"
        self.agent_id = None
        self.agent_info = None
        self.last_seen = time.time()
        self.send_lock = threading.Lock()
        self.closed = False

    def send_raw(self, raw_packet: bytes) -> bool:
        if self.closed:
            return False
        try:
            with self.send_lock:
                self.sock.sendall(raw_packet)
            return True
        except Exception:
            self.close()
            return False

    def close(self):
        if self.closed:
            return
        self.closed = True
        try:
            self.sock.close()
        except Exception:
            pass


class LanControlGatewayServer:
    def __init__(self, port: int = 8443):
        self.port = port
        self.agents = {}     # agent_id -> ClientSession
        self.controls = []   # list of ClientSession
        self.lock = threading.Lock()
        self.running = True

    def start(self):
        srv = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        srv.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        srv.bind(("0.0.0.0", self.port))
        srv.listen(128)

        print("===================================================================")
        print("  LAN CONTROL CENTRAL GATEWAY SERVER v2.2.0 (LINUX / MACOS)        ")
        print(f"  Đang lắng nghe trên: 0.0.0.0:{self.port} (Multi-VLAN Ready)      ")
        print("===================================================================")

        threading.Thread(target=self._cleanup_stale_loop, daemon=True).start()

        while self.running:
            try:
                client_sock, addr = srv.accept()
                client_sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
                sess = ClientSession(client_sock, addr)
                threading.Thread(target=self._client_loop, args=(sess,), daemon=True).start()
            except Exception as ex:
                if self.running:
                    print(f"[WARN] Accept error: {ex}")

    def _client_loop(self, sess: ClientSession):
        try:
            while self.running and not sess.closed:
                ptype, json_str, raw_bytes, full_packet = decode_packet(sess.sock)
                sess.last_seen = time.time()
                self._process_packet(sess, ptype, json_str, raw_bytes, full_packet)
        except Exception:
            pass
        finally:
            self._handle_disconnect(sess)

    def _process_packet(self, sess: ClientSession, ptype: int, json_str: str, raw_bytes: bytes, full_packet: bytes):
        if ptype == PacketType.Register:
            if '"AgentId"' in json_str:
                info = json.loads(json_str)
                aid = info.get("AgentId") or uuid.uuid4().hex
                sess.client_type = "agent"
                sess.agent_id = aid
                sess.agent_info = info
                with self.lock:
                    old = self.agents.get(aid)
                    if old and old is not sess:
                        old.close()
                    self.agents[aid] = sess
                print(f"[{datetime.now().strftime('%H:%M:%S')}] [AGENT+] {info.get('Hostname')} ({info.get('IpAddress')}) - VLAN: {info.get('VlanTag')}")
                self._broadcast_agent_list()
            else:
                sess.client_type = "control"
                with self.lock:
                    if sess not in self.controls:
                        self.controls.append(sess)
                print(f"[{datetime.now().strftime('%H:%M:%S')}] [CONTROL+] Master Console kết nối từ {sess.addr[0]}:{sess.addr[1]}")
                self._send_agent_list_to(sess)

        elif ptype == PacketType.Heartbeat:
            if sess.client_type == "agent" and json_str:
                try:
                    upd = json.loads(json_str)
                    if sess.agent_info:
                        sess.agent_info.update(upd)
                except Exception:
                    pass
                self._forward_to_controls(full_packet)

        elif ptype in (PacketType.ScreenFrame, PacketType.CommandResponse, PacketType.HelpRequest,
                       PacketType.FileTransferResponse, PacketType.FileListResponse):
            self._forward_to_controls(full_packet)

        elif ptype in (PacketType.StartScreenStream, PacketType.StopScreenStream):
            target_id = json_str.strip().strip('"')
            self._send_to_agent(target_id, full_packet)

        elif ptype == PacketType.InputEvent:
            try:
                ev = json.loads(json_str)
                self._send_to_agent(ev.get("AgentId", ""), full_packet)
            except Exception:
                pass

        elif ptype == PacketType.CommandRequest:
            try:
                req = json.loads(json_str)
                aid = req.get("AgentId", "")
                vlan = req.get("TargetVlan", "")
                if aid:
                    self._send_to_agent(aid, full_packet)
                else:
                    self._send_to_vlan(vlan, full_packet)
            except Exception:
                pass

        elif ptype in (PacketType.StartBroadcast, PacketType.BroadcastFrame, PacketType.StopBroadcast):
            target_filter = (json_str or "").strip().strip('"')
            if target_filter.upper().startswith("AGENTS:"):
                raw_ids = target_filter[len("AGENTS:"):].replace(";", ",").split(",")
                id_set = {x.strip().lower() for x in raw_ids if x.strip()}
                with self.lock:
                    targets = [a for aid, a in self.agents.items() if aid.lower() in id_set]
                for a in targets:
                    a.send_raw(full_packet)
            else:
                self._send_to_vlan(target_filter, full_packet)

        elif ptype == PacketType.HelpResponse:
            try:
                resp = json.loads(json_str)
                self._send_to_agent(resp.get("AgentId", ""), full_packet)
            except Exception:
                pass

        elif ptype in (PacketType.FileTransferRequest, PacketType.FileListRequest):
            try:
                req = json.loads(json_str)
                aid = req.get("AgentId", "")
                vlan = req.get("TargetVlan", "")
                if aid:
                    self._send_to_agent(aid, full_packet)
                else:
                    self._send_to_vlan(vlan, full_packet)
            except Exception:
                pass

        elif ptype == PacketType.FileTransferChunk:
            if sess.client_type == "agent":
                self._forward_to_controls(full_packet)
            else:
                try:
                    meta = json.loads(json_str)
                    self._send_to_agent(meta.get("AgentId", ""), full_packet)
                except Exception:
                    pass

    def _send_to_agent(self, agent_id: str, raw_packet: bytes):
        if not agent_id:
            return
        with self.lock:
            ag = self.agents.get(agent_id)
        if ag:
            ag.send_raw(raw_packet)

    def _send_to_vlan(self, vlan_filter: str, raw_packet: bytes):
        all_mode = (not vlan_filter) or (vlan_filter.upper() == "ALL")
        with self.lock:
            agents_list = list(self.agents.values())
        for ag in agents_list:
            if all_mode or (ag.agent_info and ag.agent_info.get("VlanTag", "").lower() == vlan_filter.lower()):
                ag.send_raw(raw_packet)

    def _forward_to_controls(self, raw_packet: bytes):
        with self.lock:
            ctrls = list(self.controls)
        for c in ctrls:
            c.send_raw(raw_packet)

    def _get_agent_info_list(self):
        with self.lock:
            return [a.agent_info for a in self.agents.values() if a.agent_info is not None]

    def _send_agent_list_to(self, ctrl: ClientSession):
        pkt = encode_packet(PacketType.AgentList, self._get_agent_info_list())
        ctrl.send_raw(pkt)

    def _broadcast_agent_list(self):
        pkt = encode_packet(PacketType.AgentList, self._get_agent_info_list())
        self._forward_to_controls(pkt)

    def _handle_disconnect(self, sess: ClientSession):
        sess.close()
        changed = False
        with self.lock:
            if sess.client_type == "agent" and sess.agent_id:
                if self.agents.get(sess.agent_id) is sess:
                    del self.agents[sess.agent_id]
                    changed = True
                    print(f"[{datetime.now().strftime('%H:%M:%S')}] [AGENT-] Máy trạm ngắt kết nối: {sess.agent_id[:8]}")
            elif sess in self.controls:
                self.controls.remove(sess)
        if changed:
            self._broadcast_agent_list()

    def _cleanup_stale_loop(self):
        while self.running:
            time.sleep(5.0)
            now = time.time()
            stale = []
            with self.lock:
                for aid, ag in list(self.agents.items()):
                    if now - ag.last_seen > 15.0:
                        stale.append(ag)
            for ag in stale:
                self._handle_disconnect(ag)


if __name__ == "__main__":
    port = 8443
    base_dir = os.path.dirname(os.path.abspath(__file__))
    cfg_file = os.path.join(base_dir, "gateway_config.json")
    if os.path.exists(cfg_file):
        try:
            with open(cfg_file, "r", encoding="utf-8") as f:
                d = json.load(f)
                port = int(d.get("Port", d.get("ListenPort", 8443)))
        except Exception:
            pass
    if len(sys.argv) >= 2:
        try:
            port = int(sys.argv[1])
        except Exception:
            pass

    server = LanControlGatewayServer(port)
    try:
        server.start()
    except KeyboardInterrupt:
        print("\n[INFO] Đã dừng LAN Control Gateway Server.")
