"""Build pinned third-party code only in a task-specific cache, never in Server/."""
import hashlib, io, json, os, subprocess, tarfile, urllib.request
from pathlib import Path
CACHE = Path.home()/'.cache/gorilla-wt-spike'
SHA = 'b63ebb06b0ab33af73c9bf1f96bedd9e7cb73243'
JAVA = Path(os.environ.get('WT_JAVA_HOME', '/usr/lib/jvm/java-21-openjdk-amd64'))
def fetch(url):
    return urllib.request.urlopen(url, timeout=60).read()
def extract(data, dest):
    with tarfile.open(fileobj=io.BytesIO(data), mode='r:gz') as tar:
        tar.extractall(dest, filter='data')
def prepare():
    CACHE.mkdir(parents=True, exist_ok=True)
    source = CACHE/('netty-webtransport-'+SHA)
    if not source.exists():
        data=fetch('https://codeload.github.com/suboptimal-solutions/netty-webtransport/tar.gz/'+SHA)
        if hashlib.sha256(data).hexdigest()!='2c365a76475898d0c9730e06af29ed744c52bbbceddabc0832df0a3d6d887de5':raise ValueError('source archive checksum')
        extract(data,CACHE)
        (CACHE/'source-archive-sha256.txt').write_text(hashlib.sha256(data).hexdigest()+'\n')
    maven=CACHE/'apache-maven-3.9.11/bin/mvn'
    if not maven.exists():
        url='https://repo.maven.apache.org/maven2/org/apache/maven/apache-maven/3.9.11/apache-maven-3.9.11-bin.tar.gz'
        data=fetch(url)
        assert hashlib.sha512(data).hexdigest()==fetch(url+'.sha512').decode().split()[0]
        extract(data,CACHE)
    env=dict(os.environ,JAVA_HOME=str(JAVA),PATH=str(JAVA/'bin')+os.pathsep+os.environ['PATH'])
    args=[str(maven),'-B','-ntp','-Dmaven.repo.local='+str(CACHE/'m2')]
    subprocess.run(args+['-pl','netty-codec-webtransport','-am','-DskipTests','install'],cwd=source,env=env,check=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
    subprocess.run(args+['package','dependency:build-classpath','-Dmdep.outputFile=target/classpath.txt'],cwd=Path(__file__).parent,env=env,check=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
    return CACHE
if __name__=='__main__':
    try: prepare()
    except subprocess.CalledProcessError as error:
        print(error.stdout.decode()[-8000:]); raise SystemExit(error.returncode)
