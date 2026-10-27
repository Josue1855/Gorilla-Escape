# Preparación para respaldar Desafío del Tronco — 2026-10-26

Rama: feature/gameplay-desafio-del-tronco. Se publican las mejoras locales del juego, interfaz Unity/PWA/Flutter, PIN local, descubrimiento LAN, tests y recursos compartidos con sus licencias. No es merge a develop/main ni declaración de release lista.

Validaciones realmente ejecutadas sobre estas fuentes: PWA87/87 y build PASS; Flutter22/22 y analyze sin problemas; Java verify/build PASS con118 ejecutadas y5 omitidas por requisitos de entorno, detallados en validation.json; firewall11/11, foundation y management PASS. Los builds/cachés generados no se incluyen en Git. El servidor se compiló tras el build PWA.

Unity6000.3.23f1 no inició las pruebas EditMode: exit198 por falta de licencia válida. No se ejecutó PlayMode ni se generó un nuevo Player. No se eludió la licencia ni se presentan los informes históricos como una nueva aprobación. Antes de integrar es necesario restaurar el entorno autorizado y ejecutar las pruebas Unity afectadas, junto con las comprobaciones físicas pendientes.

Se revisaron las capturas existentes para publicación: no incluyen un QR de admisión ni tokens visibles; el PIN mostrado es ilustrativo. El log Avahi original con identificadores LAN permanece local; se publica una copia marcada como anonimizada y explicada junto a ella. La carpeta Unity/Assets/_Recovery se conserva local y se excluye de Git.

source-sha256.json identifica las fuentes/recursos funcionales seleccionados. Los resultados históricos del22/23 de octubre no se modifican ni se ocultan sus fallos. No se fabrica evidencia de iPhone/Android físico o PC limpia. El objetivo de esta publicación es respaldar el trabajo en su rama para revisión posterior.
