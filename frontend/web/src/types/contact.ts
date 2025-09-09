export type ContactCreateDto = {
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  tags?: string[];
};

export type ContactReadDto = {
  id: string;
  organizationId?: string;
  firstName?: string;
  lastName?: string;
  name?: string;
  email?: string;
  phone?: string;
  tags?: string[];
  createdAt?: string;
  ownerId?: string;
};
