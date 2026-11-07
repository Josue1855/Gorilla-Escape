import socket
import struct
import threading
import time
import unittest
from pipeline import CapturedFrame, Metrics
from preview import Preview


class PreviewRecoveryTests(unittest.TestCase):
    def test_each_image_is_sent_once_and_a_new_client_can_recover(self):
        preview = Preview(threading.Event(), Metrics(), encoder=lambda image: image)
        client = socket.create_connection(('127.0.0.1', preview.port), timeout=2)
        client.settimeout(2)

        def poll(sock):
            sock.sendall(b'P')
            header = b''
            while len(header) < 4:
                header += sock.recv(4 - len(header))
            length = struct.unpack('!I', header)[0]
            data = b''
            while len(data) < length:
                data += sock.recv(length - len(data))
            return data

        try:
            self.assertEqual(b'', poll(client))
            preview.publish(CapturedFrame(1, b'frame-one', 1, 1))
            deadline = time.monotonic() + 2
            first = b''
            while not first and time.monotonic() < deadline:
                first = poll(client)
            self.assertEqual(b'frame-one', first)
            self.assertEqual(b'', poll(client))
            client.close()
            deadline = time.monotonic() + 2
            while preview.enabled.is_set() and time.monotonic() < deadline:
                time.sleep(.01)
            client = socket.create_connection(('127.0.0.1', preview.port), timeout=2)
            client.settimeout(2)
            self.assertEqual(b'', poll(client))
            preview.publish(CapturedFrame(2, b'frame-two', 2, 2))
            deadline = time.monotonic() + 2
            second = b''
            while not second and time.monotonic() < deadline:
                second = poll(client)
            self.assertEqual(b'frame-two', second)
        finally:
            client.close()
            preview.close()


if __name__ == '__main__':
    unittest.main()
