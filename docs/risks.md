# CleanStats — riesgos y mitigaciones

| Riesgo | Mitigación inicial |
|---|---|
| Borrar datos personales por error | No hay eliminación en el MVP; toda clasificación es explicable. |
| Confundir antigüedad con inutilidad | La fecha solo sirve como señal; nunca basta para clasificar basura. |
| Fecha de último acceso poco fiable | Mostrarla como opcional y advertir que Windows puede retrasar/desactivar su actualización. |
| Permisos insuficientes | Registrar omisiones y continuar cuando sea seguro. |
| Ciclos o duplicación por enlaces | No seguir puntos de reanálisis por defecto. |
| Archivos cambian durante el escaneo | Marcar estado parcial/cambiado y revalidar en etapas posteriores. |
| Diferencia entre tamaño lógico y físico | Mantener campos separados y documentar cuándo el tamaño asignado es desconocido. |
| Uso excesivo de memoria | Diseñar enumeración incremental, agregación por lotes y UI virtualizada. |
| API nativa incompatible o con permisos elevados | Mantener proveedor convencional como alternativa; posponer optimización NTFS. |
| Datos privados en logs | No registrar contenidos; minimizar rutas y datos sensibles en registros diagnósticos. |
