export const normalizeRoomCode = value => typeof value==='string' && /^[2-9A-HJKMNP-Z]{4}$/i.test(value.trim())?value.trim().toUpperCase():null;
