import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec=importlib.util.spec_from_file_location('firewall',Path(__file__).with_name('firewall.py'))
f=importlib.util.module_from_spec(spec);spec.loader.exec_module(f)
NET={'interface':'wlan0','address':'192.168.1.5','subnet':'192.168.1.0/24'}

def line(rule):
    return '### tuple ### allow %s %s %s any %s in_%s comment=%s\n' % (rule[5],rule[11],rule[9],rule[7],rule[3],rule[-1].encode().hex())

class FirewallTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name);self.db=self.root/'user.rules';self.db.write_text('')
        self.state=self.root/'state';self.state.mkdir();self.calls=[]
    def run_cmd(self,command):
        self.calls.append(command)
        if command[1:3]==['--force','delete']:
            rule=command[3:];self.db.write_text(self.db.read_text().replace(line(rule),''))
        else:self.db.write_text(self.db.read_text()+line(command[1:]))
        return ''
    def execute(self,op,recovery=False,net=NET):
        return f.execute(op,net,recovery,self.state,self.run_cmd,(self.db,))
    def test_missing_check_is_read_only(self):
        self.assertEqual('NEEDS_PERMISSION',self.execute('--check'));self.assertEqual([],self.calls)
    def test_existing_rules_do_not_need_install(self):
        self.db.write_text(''.join(map(line,f.rules(NET))))
        self.assertEqual('READY',self.execute('--check'));self.assertEqual([],self.calls)
    @patch.object(f.os,'geteuid',return_value=0)
    def test_install_second_run_and_restart_do_not_duplicate(self,_):
        with patch.object(f,'safe_receipt',side_effect=lambda p:json.loads(p.read_text()) if p.exists() else None):
            self.assertEqual('READY',self.execute('--install'));self.assertEqual(2,len(self.calls))
            self.assertEqual('READY',self.execute('--check'));self.execute('--install');self.assertEqual(2,len(self.calls))
    @patch.object(f.os,'geteuid',return_value=0)
    def test_remove_preserves_preexisting_equivalent_and_foreign_rules(self,_):
        foreign=line(f.rules(NET)[0]).replace('GorillaEscape-v1-https'.encode().hex(),'operator'.encode().hex())
        self.db.write_text(foreign)
        with patch.object(f,'safe_receipt',side_effect=lambda p:json.loads(p.read_text()) if p.exists() else None):
            self.execute('--install');self.execute('--remove');self.assertEqual(foreign,self.db.read_text())
    @patch.object(f.os,'geteuid',return_value=0)
    def test_replaced_rule_is_foreign_even_when_our_comment_exists_elsewhere(self,_):
        with patch.object(f,'safe_receipt',side_effect=lambda p:json.loads(p.read_text()) if p.exists() else None):
            self.execute('--install')
            own=f.rules(NET)[0]
            foreign=line(own).replace(own[-1].encode().hex(),'operator'.encode().hex())
            other=own.copy();other[11]='9443'
            # Administrator replaced our rule; the original comment now identifies a different tuple.
            self.db.write_text(self.db.read_text().replace(line(own),foreign)+line(other))
            self.execute('--remove')
            self.assertIn(foreign,self.db.read_text());self.assertIn(line(other),self.db.read_text())

    @patch.object(f.os,'geteuid',return_value=0)
    def test_failed_install_rolls_back_only_new_rules(self,_):
        calls=self.run_cmd
        def fail(command):
            if 'udp' in command and '--force' not in command:raise f.FirewallError('TEST_FAILURE')
            return calls(command)
        with patch.object(f.os,'geteuid',return_value=0):
            with self.assertRaises(f.FirewallError):f.execute('--install',NET,False,self.state,fail,(self.db,))
        self.assertEqual('',self.db.read_text())
    def test_network_change_requires_new_rules(self):
        self.db.write_text(''.join(map(line,f.rules(NET))))
        self.assertEqual('NEEDS_PERMISSION',self.execute('--check',net={**NET,'interface':'wlan1','subnet':'192.168.2.0/24','address':'192.168.2.5'}))
    @patch.object(f.os,'geteuid',return_value=0)
    def test_recovery_removal_preserves_main_rules(self,_):
        with patch.object(f,'safe_receipt',side_effect=lambda p:json.loads(p.read_text()) if p.exists() else None):
            self.execute('--install');self.execute('--install',True);self.execute('--remove',True)
            self.assertEqual(''.join(map(line,f.rules(NET))),self.db.read_text())
    def test_rules_are_interface_subnet_destination_and_allocator_bounded(self):
        for r in f.rules(NET):self.assertIn('wlan0',r);self.assertIn('192.168.1.0/24',r);self.assertIn('192.168.1.5',r)
        self.assertNotIn('8081',sum(f.rules(NET),[]));self.assertIn('8081',sum(f.rules(NET,True),[]))
        java=(Path(__file__).parents[2]/'Server/src/main/java/com/gorillaescape/server/mobile/MobileRuntime.java').read_text()
        self.assertIn('RTC_MIN_PORT=%d, RTC_MAX_PORT=%d'%(f.RTC_MIN,f.RTC_MAX),java)
    def test_invalid_address_or_interface_is_never_command_text(self):
        for address in ['8.8.8.8','127.0.0.1','192.168.1.5;id','$(id)']:
            with self.assertRaises((ValueError,f.FirewallError)):f.network(address,lambda _: '[]')
        data=[{'ifname':'wlan0;id','addr_info':[{'local':'192.168.1.5','prefixlen':24}]}]
        with self.assertRaises(f.FirewallError):f.network('192.168.1.5',lambda _:json.dumps(data))
    def test_root_receipt_rejects_user_owned_or_writable_state(self):
        p=self.root/'fake.json';p.write_text('{}');self.assertIsNone(f.safe_receipt(p))

if __name__=='__main__':unittest.main()
